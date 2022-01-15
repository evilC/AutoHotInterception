using AutoHotInterception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    class MouseButtonTester
    {
        public MouseButtonTester(TestDevice device, MouseButton btn, bool block = false)
        {
            Console.WriteLine($"Test button: {btn.Name}");
            var im = new Manager();

            var devId = device.GetDeviceId();

            if (devId == 0) return;

            im.SubscribeMouseButton(devId, btn.Code, block, new Action<int>(OnButtonEvent));
        }

        public void OnButtonEvent(int value)
        {
            Console.WriteLine($"State: {value}");
        }
    }
}
