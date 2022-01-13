using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHotInterception;

namespace TestApp
{
    public class TabletTester
    {
        public TabletTester(TestDevice device)
        {
            var im = new Manager();

            var devId = device.GetDeviceId();
            if (devId == 0) return;
            var counter = 0;

            if (devId != 0)
            {
                Console.WriteLine($"Testing Absolute device with ID of {devId}");
                im.SubscribeMouseButton(devId, 0, true, new Action<int>(value =>
                {
                    Console.WriteLine("LButton Button Value: " + value);
                }));
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

                im.SubscribeMouseMoveAbsolute(devId, true, new Action<int, int>((x, y) =>
                {
                    Console.WriteLine($"x: {x}, y: {y}");
                }));
            }
        }
    }
}
