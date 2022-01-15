using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHotInterception;

namespace TestApp
{
    public class KeyboardTester
    {
        public KeyboardTester(TestDevice device, bool block = false)
        {
            var im = new Manager();

            var devId = device.GetDeviceId();

            if (devId == 0) return;

            im.SubscribeKeyboard(devId, block, new Action<ushort, int>(OnKeyEvent));
        }

        public void OnKeyEvent(ushort code, int value)
        {
            var keyObj = AhkKeys.Obj(code);

            Console.WriteLine($"Name: {keyObj.Name}, Code: {keyObj.LogCode()}, State: {value}");
        }
    }
}
