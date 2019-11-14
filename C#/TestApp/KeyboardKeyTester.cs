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

            var devId = im.GetKeyboardId(0x03EB, 0xFF02);

            if (devId == 0) return;

            im.SubscribeKey(devId, 0x1, false, new Action<int>(value =>
            {
                Console.WriteLine($"State: {value}");
            }));
        }

        public void OnKeyEvent(int value)
        {
            Console.WriteLine($"State: {value}");
        }

    }
}
