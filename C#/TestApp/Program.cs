using System;
using AutoHotInterception;

namespace TestApp
{
    internal class TestApp
    {
        private static void Main()
        {
            var im = new Manager();

            var mouseHandle = "HID\\VID_046D&PID_C52B&REV_2407&MI_02&Qid_1028&WI_01&Class_00000004";
            var mouseId = im.GetMouseIdFromHandle(mouseHandle);

            var counter = 0;

            if (mouseId != 0)
            {
                im.SubscribeMouseButton(mouseId, 1, true, new Action<int>(value =>
                {
                    //Console.WriteLine("RButton Button Value: " + value);
                }));
                im.SubscribeMouseButton(mouseId, 3, true, new Action<int>(value =>
                {
                    //Console.WriteLine("XButton1 Button Value: " + value);
                }));
                im.SubscribeMouseButton(mouseId, 4, true, new Action<int>(value =>
                {
                    //Console.WriteLine("XButton2 Button Value: " + value);
                }));
                im.SubscribeMouseButton(mouseId, 5, true, new Action<int>(value =>
                {
                    //Console.WriteLine("WheelVertical Value: " + value);
                    var mycounter = counter;
                    mycounter++;
                    Console.WriteLine("Counter: " + mycounter);
                    counter = mycounter;
                }), false);
            }

            Console.ReadLine();
        }
    }
}