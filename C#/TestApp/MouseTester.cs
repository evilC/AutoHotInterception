using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHotInterception;

namespace TestApp
{
    public class MouseTester
    {
        public MouseTester()
        {
            var im = new Manager();

            var mouseHandle = "HID\\VID_046D&PID_C52B&REV_2407&MI_02&Qid_1028&WI_01&Class_00000004";
            var devId = im.GetMouseIdFromHandle(mouseHandle);

            var counter = 0;

            if (devId != 0)
            {
                im.SubscribeMouseButton(devId, 1, true, new Action<int>(value =>
                {
                    Console.WriteLine("RButton Button Value: " + value);
                }));
                im.SubscribeMouseButton(devId, 3, true, new Action<int>(value =>
                {
                    Console.WriteLine("XButton1 Button Value: " + value);
                }));
                im.SubscribeMouseButton(devId, 4, true, new Action<int>(value =>
                {
                    Console.WriteLine("XButton2 Button Value: " + value);
                }));
                im.SubscribeMouseButton(devId, 5, true, new Action<int>(value =>
                {
                    Console.Write("WheelVertical Value: " + value);
                    var mycounter = counter;
                    mycounter++;
                    Console.WriteLine(" Counter: " + mycounter);
                    counter = mycounter;
                }));
            }

            Console.ReadLine();
        }
    }
}
