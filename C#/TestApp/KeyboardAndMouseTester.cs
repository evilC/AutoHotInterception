using AutoHotInterception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestApp.Helpers;

namespace TestApp
{
    /// <summary>
    /// Allows testing of Keyboard all keys and mouse movement at the same time
    /// </summary>
    public class KeyboardAndMouseTester
    {
        private Manager im = new Manager();

        public KeyboardAndMouseTester(TestDevice testDevice, bool block = false)
        {
            AddDevice(testDevice, block);
        }

        public KeyboardAndMouseTester AddDevice(TestDevice testDevice, bool block = false)
        {
            var devId = testDevice.GetDeviceId();

            if (devId == 0) return this;
            if (devId < 11)
            {
                im.SubscribeKeyboard(devId, block, new Action<ushort, int>((code, value) =>
                {
                    var keyObj = AhkKeys.Obj(code);

                    Console.WriteLine($"Name: {keyObj.Name}, Code: {keyObj.LogCode()}, State: {value}");
                }));
            }
            else
            {
                im.SubscribeMouseMove(devId, block, new Action<int, int>((x, y) =>
                {
                    Console.WriteLine($"Mouse Move: x: {x}, y: {y}");
                }));
            }
            return this;
        }
    }
}
