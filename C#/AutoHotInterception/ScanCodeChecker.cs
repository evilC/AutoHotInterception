using System;
using System.Collections.Generic;
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
            while (ManagedWrapper.Receive(_deviceContext, i = ManagedWrapper.Wait(_deviceContext), ref stroke, 1) > 0)
            {
                if (!_block) ManagedWrapper.Send(_deviceContext, _deviceId, ref stroke, 1);
                _callback(new KeyEvent { Code = stroke.key.code, State = stroke.key.state });
            }
        }

        public string OkCheck()
        {
            return "OK";
        }

        private int IsMonitoredDevice(int device)
        {
            return Convert.ToInt32(_deviceId == device);
        }
    }

    public class KeyEvent
    {
        public ushort Code { get; set; }
        public ushort State { get; set; }
    }
}
