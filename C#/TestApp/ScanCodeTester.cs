using System;
using System.Collections.Generic;
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
            scc.Subscribe(devId, new Action<List<KeyEvent>>(OnKeyEvent), block);
        }

        public void OnKeyEvent(List<KeyEvent> keyEvents)
        {
            var str = $"{keyEvents.Count} - ";
            foreach (var keyEvent in keyEvents)
            {
                str += $"Code: {keyEvent.Code} (0x{keyEvent.Code.ToString("X")}) - {keyEvent.Code + 256}, State: {keyEvent.State} | ";
            }
            Debug.WriteLine(str);
        }
    }
}
