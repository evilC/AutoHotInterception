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
                new Action<int, int, int, string>((id, code, value, info) =>
                {
                    Console.WriteLine($"Mouse: ID={id}, Code={code}, Value={value}, Info={info}");
                })
            );
            var devId = mon.GetKeyboardId(0x04F2, 0x0112);
            mon.SetDeviceFilterState(devId, true);

        }
    }
}
