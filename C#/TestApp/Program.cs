using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoHotInterception;

class TestApp
{
    static void Main(string[] args)
    {
        //var mon = new AutoHotInterception.Monitor();
        //var devInfo = mon.GetDeviceList();
        //mon.Subscribe(new Action<int, ushort, ushort, uint>((id, state, code, info) =>
        //{
        //    Console.WriteLine($"Subscription Value: State={state}, Code={code}");
        //}), new Action<int, ushort, ushort, short, int, int, uint>((id, state, flags, rolling, x, y, info) =>
        //{
        //    Console.WriteLine($"Subscription Value: x={x}, y={y}");
        //}));
        //mon.SetDeviceFilterState(16, true);
        //Console.ReadLine();
        //return;

        // --------------------------------------------------------------

        var im = new Manager();

        var keyboardId = 0;
        //keyboardId = im.GetDeviceId(false, 0x04F2, 0x0112);     // WYSE
        //keyboardId = im.GetDeviceId(false, 0x413C, 0x2107);     // Dell

        var mouseId = 0;
        //mouseId = im.GetDeviceId(true, 0x46D, 0xC531);      // G700s
        //mouseId = im.GetDeviceId(true, 0x46D, 0xC00C);      // Logitech Wired
        mouseId = im.GetDeviceId(true, 0xB57, 0x9091);      // Parblo Tablet

        //im.SendMouseButtonEvent(mouseId, 1, 1);
        //Thread.Sleep(100);
        //im.SendMouseButtonEvent(mouseId, 1, 0);
        //im.SendMouseMoveRelative(mouseId, 100, 100);
        //im.SendMouseMoveAbsolute(mouseId, 100, 100);

        if (keyboardId != 0)
        {
            im.SubscribeKey(keyboardId, 2, true, new Action<int>((value) =>
            {
                Console.WriteLine("Subscription Value: " + value);
            }));

            im.SetContextCallback(keyboardId, new Action<int>((value) =>
            {
                Console.WriteLine("Context Value: " + value);
            }));
        }

        if (mouseId != 0)
        {
            im.SubscribeMouseButton(mouseId, 1, true, new Action<int>((value) =>
            {
                Console.WriteLine("Mouse Button Value: " + value);
            }));

            im.SubscribeMouseMoveRelative(mouseId, false, new Action<int, int>((x, y) =>
            {
                Console.WriteLine($"Mouse Axis Value: x={x}, y={y}");
            }));

        }
        Console.ReadLine();
    }
}
