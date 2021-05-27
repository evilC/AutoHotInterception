using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHotInterception;
using AutoHotInterception.Helpers;

namespace TestApp
{
    public class ScanCodeTester
    {
        public ScanCodeTester()
        {
            var scc = new ScanCodeChecker();
            int vid = 0x04F2, pid = 0x0112; // Wyse Keyboard
            scc.Subscribe(vid, pid, new Action<KeyEvent[]>(OnKeyEvent));
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
