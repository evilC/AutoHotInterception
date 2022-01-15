using System;
using System.Diagnostics;
using AutoHotInterception;

namespace TestApp
{
    public class ScanCodeTester
    {
        public ScanCodeTester(TestDevice device, bool block = false)
        {
            var scc = new ScanCodeChecker();
            var devId = device.GetDeviceId();
            if (devId == 0) return;
            scc.Subscribe(devId, new Action<KeyEvent>(OnKeyEvent), block);
        }

        public void OnKeyEvent(KeyEvent keyEvent)
        {
            Debug.WriteLine($"Code: {keyEvent.Code} (0x{keyEvent.Code.ToString("X")}) - {keyEvent.Code + 256}, State: {keyEvent.State}");
        }
    }
}
