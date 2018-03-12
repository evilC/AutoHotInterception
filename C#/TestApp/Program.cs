using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//WYSE VID 0x04F2 PID 0x0112
//Dell VID 0x413C PID 0x2107
class TestApp
{
    static void Main(string[] args)
    {
        var iw = new InterceptionWrapper();
        var str = iw.GetDeviceList();
        // 0x2D = X key
        iw.SubscribeKey(0x2D, true, new Action<int>((value) =>
        {
            Console.WriteLine("Subscription Value: " + value);
        //}), 0x413C, 0x2107);
        }), 0x04F2, 0x0112);
        //}), 0x046D, 0xC531);

        iw.SetContextCallback(0x04F2, 0x0112, new Action<int>((value) =>
        {
            Console.WriteLine("Context Value: " + value);
        }));
        while (true)
        {
            Thread.Sleep(100);
        }

    }
}
