using System;
using System.Collections.Generic;
using AutoHotInterception.Helpers;

namespace AutoHotInterception
{
    /*
     * Tool to check Scan Codes and Press / Release states
     */
    public class ScanCodeChecker
    {
        private readonly IntPtr _deviceContext;
        private int _filteredDevice;
        private dynamic _callback;

        public ScanCodeChecker()
        {
            _deviceContext = ManagedWrapper.CreateContext();
        }

        public void Subscribe(int vid, int pid, dynamic callback)
        {
            _callback = callback;
            _filteredDevice = HelperFunctions.GetDeviceId(_deviceContext, false, vid, pid, 1);
            if (_filteredDevice == 0)
            {
                throw new Exception($"Could not find device with VID {vid}, PID {pid}");
            }

            ManagedWrapper.SetFilter(_deviceContext, IsMonitoredDevice, ManagedWrapper.Filter.All);
            int i;
            var stroke = new ManagedWrapper.Stroke();
            while (ManagedWrapper.Receive(_deviceContext, i = ManagedWrapper.Wait(_deviceContext), ref stroke, 1) > 0)
            {
                var keyEvents = new List<KeyEvent>();
                keyEvents.Add(new KeyEvent { Code = stroke.key.code, State = stroke.key.state });
                ManagedWrapper.Send(_deviceContext, _filteredDevice, ref stroke, 1);
                _callback(keyEvents.ToArray());
            }
        }

        public string OkCheck()
        {
            return "OK";
        }

        private int IsMonitoredDevice(int device)
        {
            return Convert.ToInt32(_filteredDevice == device);
        }
    }

    public class KeyEvent
    {
        public ushort Code { get; set; }
        public ushort State { get; set; }
    }
}
