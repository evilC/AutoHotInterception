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

            im.SubscribeKey(devId, 0x2, false, new Action<int>(value =>
            {
                Console.WriteLine($"State: {value}");
            }));
        }
    }
}
