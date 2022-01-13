using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHotInterception;

namespace TestApp
{
    public class MouseButtonsTester
    {
        public MouseButtonsTester(TestDevice device)
        {
            var im = new Manager();

            var devId = device.GetDeviceId();

            if (devId != 0)
            {
                im.SubscribeMouseButtons(devId, true, new Action<ushort, int>(OnButtonEvent));
            }
        }

        public void OnButtonEvent(ushort code, int state)
        {
            Console.WriteLine($"Code: {code}, State: {state}");
        }
    }
}
