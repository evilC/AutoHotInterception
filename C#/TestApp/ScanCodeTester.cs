using System;
using System.Diagnostics;
using AutoHotInterception;

namespace TestApp
{
    public class ScanCodeTester
    {
        public ScanCodeTester(TestDevice device)
        {
            var scc = new ScanCodeChecker();
            var devId = device.GetDeviceId();
            if (devId == 0) return;
            scc.Subscribe(devId, new Action<KeyEvent>(OnKeyEvent));
        }

        public void OnKeyEvent(KeyEvent keyEvent)
        {
            Debug.WriteLine($"Code: {keyEvent.Code} (0x{keyEvent.Code.ToString("X")}), State: {keyEvent.State}");
        }
    }
}
