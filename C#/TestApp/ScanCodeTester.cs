using AutoHotInterception;
using System;
using System.Diagnostics;

namespace TestApp
{
	public class ScanCodeTester: IDisposable
    {
        private readonly ScanCodeChecker _scc;

        public ScanCodeTester()
        {
            _scc = new ScanCodeChecker();
            int vid = 0x04F2, pid = 0x0112; // Wyse Keyboard
            _scc.Subscribe(vid, pid, new Action<KeyEvent[]>(OnKeyEvent));
        }

        public void Dispose()
        {
            _scc.Dispose();
        }

        public void OnKeyEvent(KeyEvent[] keyEvents)
        {
            var str = "";
            foreach (var keyEvent in keyEvents)
            {
                str += $"Code: {keyEvent.Code}, State: {keyEvent.State} | ";
            }
            Debug.WriteLine(str);
        }
    }
}
