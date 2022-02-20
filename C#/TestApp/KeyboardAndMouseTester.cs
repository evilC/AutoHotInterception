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
        private Dictionary<int, bool> _deviceStates = new Dictionary<int, bool>();
        private Dictionary<int, bool> _blockingEnabled = new Dictionary<int, bool>();

        public KeyboardAndMouseTester(TestDevice testDevice, bool block = false)
        {
            AddDevice(testDevice, block);
        }

        public KeyboardAndMouseTester AddDevice(TestDevice testDevice, bool block = false)
        {
            var devId = testDevice.GetDeviceId();

            if (devId == 0) return this;
            _blockingEnabled[devId] = block;
            SetDeviceState(devId, true);
            return this;
        }

        private void SetDeviceState(int devId, bool state)
        {
            if (devId < 11)
            {
                if (state)
                {
                    im.SubscribeKeyboard(devId, _blockingEnabled[devId], new Action<ushort, int>((code, value) =>
                    {
                        var keyObj = AhkKeys.Obj(code);

                        Console.WriteLine($"Name: {keyObj.Name}, Code: {keyObj.LogCode()}, State: {value}");
                    }));
                }
                else
                {
                    im.UnsubscribeKeyboard(devId);
                }
            }
            else
            {
                if (state)
                {
                    im.SubscribeMouseMove(devId, _blockingEnabled[devId], new Action<int, int>((x, y) =>
                    {
                        Console.WriteLine($"Mouse Move: x: {x}, y: {y}");
                    }));
                }
                else
                {
                    im.UnsubscribeMouseMove(devId);
                }
            }
            _deviceStates[devId] = state;
        }

        // Allows toggling on and off of keyboard subscription whilst mouse sub active
        public void Toggle(TestDevice testDevice)
        {
            var devId = testDevice.GetDeviceId();
            while (true)
            {
                Console.WriteLine($"Subscribe: {_deviceStates[devId]} (Enter to toggle)");
                Console.ReadLine();
                SetDeviceState(devId, !_deviceStates[devId]);
            }
        }
    }
}
