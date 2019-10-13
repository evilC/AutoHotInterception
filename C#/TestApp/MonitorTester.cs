using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHotInterception;

namespace TestApp
{
    public class MonitorTester
    {
        public MonitorTester()
        {
            var mon = new Monitor();
            mon.Subscribe(new Action<int, int, int, string>((id, code, value, info) =>
                {
                    Console.WriteLine($"Keyboard: ID={id}, Code={code}, Value={value}, Info={info}");
                }),
                new Action<int, int, int, int, int, string>((id, code, value, x, y, info) =>
                {
                    Console.WriteLine($"Mouse: ID={id}, Code={code}, Value={value}, X={x}, Y={y}, Info={info}");
                })
            );
            var keyboardId = mon.GetKeyboardId(0x04F2, 0x0112);
            mon.SetDeviceFilterState(keyboardId, true);

            var mouseId = mon.GetMouseIdFromHandle(@"HID\VID_046D&PID_C00C&REV_0620");
            mon.SetDeviceFilterState(mouseId, true);
        }
    }
}
