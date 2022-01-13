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
        public KeyboardKeyTester(TestDevice device, AhkKey key)
        {
            Console.WriteLine($"Test key: {key.Name} - code {key.LogCode()}");
            var im = new Manager();

            var devId = device.GetDeviceId();

            if (devId == 0) return;

            im.SubscribeKey(devId, 0x2, false, new Action<int>(OnKeyEvent));
        }

        public void OnKeyEvent(int value)
        {
            Console.WriteLine($"State: {value}");
        }

    }
}
