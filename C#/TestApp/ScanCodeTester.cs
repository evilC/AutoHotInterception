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
    public class ScanCodeTester: IDisposable
    {
        private readonly ScanCodeChecker _scc;

        public ScanCodeTester()
        {
            _scc = new ScanCodeChecker();
            _scc.Subscribe(0x050D, 0x0200, new Action<KeyEvent[]>(OnKeyEvent));
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
