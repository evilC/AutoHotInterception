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
        public MouseButtonsTester()
        {
            var im = new Manager();

            //var devs = im.GetDeviceList();
            //var mouseHandle = @"HID\VID_046D&PID_C539&REV_3904&MI_01&Col01";
            var mouseHandle = "HID\\VID_046D&PID_C00C&REV_0620"; // Logitech USB
            var devId = im.GetMouseIdFromHandle(mouseHandle);

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
