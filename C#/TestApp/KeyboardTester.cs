using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHotInterception;

namespace TestApp
{
    public class KeyboardTester
    {
        public KeyboardTester()
        {
            var im = new Manager();

            var devId = im.GetKeyboardId(0x04F2, 0x0112);

            if (devId == 0) return;

            im.SubscribeKeyboard(devId, false, new Action<ushort, int>(OnKeyEvent));
        }

        public void OnKeyEvent(ushort code, int value)
        {
            Console.WriteLine($"Code: {code}, State: {value}");
        }
    }
}
