using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AutoHotInterception.Helpers;

namespace AutoHotInterception
{
    /*
    Tool to check Scan Codes and Press / Release states
    Note that these are raw scancodes and states as they come from Interception. Some keys (eg extended code keys) will not match AHK key codes!
    */
    public class ScanCodeChecker : IDisposable
    {
        private readonly IntPtr _deviceContext;
        private dynamic _callback;
        private int _deviceId;
        private bool _block;
        private Thread _pollThread;

        public ScanCodeChecker()
        {
            _deviceContext = ManagedWrapper.CreateContext();
        }

        public void Subscribe(int deviceId, dynamic callback, bool block = false)
        {
            _callback = callback;
            _deviceId = deviceId;
            _block = block;

            _pollThread = new Thread(PollThread);
            _pollThread.Start();
        }

        private void PollThread()
        {
            ManagedWrapper.SetFilter(_deviceContext, IsMonitoredDevice, ManagedWrapper.Filter.All);
            int deviceId1;
            int deviceId2;
            var stroke1 = new ManagedWrapper.Stroke();
            var stroke2 = new ManagedWrapper.Stroke();
            while (true)
            {
                var strokes = new List<ManagedWrapper.Stroke>();
                if (ManagedWrapper.Receive(_deviceContext, deviceId1 = ManagedWrapper.WaitWithTimeout(_deviceContext, 10), ref stroke1, 1) > 0)
                {
                    strokes.Add(stroke1);
                    if (deviceId1 < 11)
                    {
                        if (ManagedWrapper.Receive(_deviceContext, deviceId2 = ManagedWrapper.WaitWithTimeout(_deviceContext, 0), ref stroke2, 1) > 0)
                        {
                            strokes.Add(stroke2);
                        }
                    }
                    if (!_block)
                    {
                        for (int i = 0; i < strokes.Count; i++)
                        {
                            var stroke = strokes[i];
                            ManagedWrapper.Send(_deviceContext, _deviceId, ref stroke, 1);
                        }
                    }
                    // Use array for callback, as the callback may be AHK code, and dealing with arrays in AHK is way simpler that Lists
                    var keyEvents = new KeyEvent[strokes.Count];
                    for (int i = 0; i < strokes.Count; i++)
                    {
                        var s = strokes[i];
                        keyEvents[i] = new KeyEvent { Code = s.key.code, State = s.key.state };
                    }
                    _callback(keyEvents);
                }
            }
        }

        public string OkCheck()
        {
            return "OK";
        }

        private int IsMonitoredDevice(int device)
        {
            return (Convert.ToInt32(_deviceId == device) );
        }

        public void Dispose()
        {
            _pollThread.Abort();
            _pollThread.Join();
        }
    }

    public class KeyEvent
    {
        public ushort Code { get; set; }
        public ushort State { get; set; }
    }
}
