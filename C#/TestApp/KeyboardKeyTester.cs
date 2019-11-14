using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHotInterception;

namespace TestApp
{
    public class KeyboardKeyTester
    {
        public KeyboardKeyTester()
        {
            var im = new Manager();

            var devId = im.GetKeyboardId(0x04F2, 0x0112);

            if (devId == 0) return;

            im.SubscribeKey(devId, 0x2, false, new Action<int>(OnKeyEvent));
        }

        public void OnKeyEvent(int value)
        {
            Console.WriteLine($"State: {value}");
        }

    }
}
