using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutoHotInterception;

namespace TestApp
{
    public class ScanCodeTester : IDisposable
    {
        private ScanCodeChecker scc;
        public ScanCodeTester(TestDevice device, bool block = false)
        {
            scc = new ScanCodeChecker();
            var devId = device.GetDeviceId();
            if (devId == 0) return;
            scc.Subscribe(devId, new Action<KeyEvent[]>(OnKeyEvent), block);
        }

        public void Dispose()
        {
            scc.Dispose();
        }

        public void OnKeyEvent(KeyEvent[] keyEvents)
        {
            var str = "";
            foreach (var keyEvent in keyEvents)
            {
                str += $"Code: {keyEvent.Code} (0x{keyEvent.Code.ToString("X")}) - {keyEvent.Code + 256}, State: {keyEvent.State} | ";
            }
            Debug.WriteLine(str);
        }
    }
}
