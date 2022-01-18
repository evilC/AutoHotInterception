using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutoHotInterception.Helpers;

namespace AutoHotInterception
{
    /*
    Tool to check Scan Codes and Press / Release states
    Note that these are raw scancodes and states as they come from Interception. Some keys (eg extended code keys) will not match AHK key codes!
    */
    public class ScanCodeChecker
    {
        private readonly IntPtr _deviceContext;
        private dynamic _callback;
        private int _deviceId;
        private bool _block;

        public ScanCodeChecker()
        {
            _deviceContext = ManagedWrapper.CreateContext();
        }

        public void Subscribe(int deviceId, dynamic callback, bool block = false)
        {
            _callback = callback;
            _deviceId = deviceId;
            _block = block;

            ManagedWrapper.SetFilter(_deviceContext, IsMonitoredDevice, ManagedWrapper.Filter.All);
            int i;
            var stroke = new ManagedWrapper.Stroke();
            while (true)
            {
                var strokes = new List<ManagedWrapper.Stroke>();
                while (ManagedWrapper.Receive(_deviceContext, i = ManagedWrapper.WaitWithTimeout(_deviceContext, 0), ref stroke, 1) > 0)
                {
                    strokes.Add(stroke);
                }
                if (!block)
                {
                    foreach (var s in strokes)
                    {
                        ManagedWrapper.Send(_deviceContext, _deviceId, ref stroke, 1);
                    }
                }
                if (strokes.Count == 0) continue;
                var keyEvents = new List<KeyEvent>();
                foreach (var s in strokes)
                {
                    keyEvents.Add(new KeyEvent { Code = s.key.code, State = s.key.state });
                }
                _callback(keyEvents);
            }
        }

        public string OkCheck()
        {
            return "OK";
        }

        private int IsMonitoredDevice(int device)
        {
            return (Convert.ToInt32(_deviceId == device) );
            //return (Convert.ToInt32(_deviceId == device || device == 12) );
        }
    }

    public class KeyEvent
    {
        public ushort Code { get; set; }
        public ushort State { get; set; }
    }
}
