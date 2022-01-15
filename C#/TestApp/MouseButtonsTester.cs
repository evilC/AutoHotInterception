using System;
using AutoHotInterception;
using TestApp.Helpers;

namespace TestApp
{
    public class MouseButtonsTester
    {
        public MouseButtonsTester(TestDevice device, bool block = false)
        {
            var im = new Manager();

            var devId = device.GetDeviceId();

            if (devId != 0)
            {
                im.SubscribeMouseButtons(devId, block, new Action<ushort, int>(OnButtonEvent));
            }
        }

        public void OnButtonEvent(ushort code, int state)
        {
            Console.WriteLine($"Code: {code}, Name: {MouseButtons.Name[code]}, State: {state}");
        }
    }
}
