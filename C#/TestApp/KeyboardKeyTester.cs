using System;
using AutoHotInterception;
using TestApp.Helpers;

namespace TestApp
{
    public class KeyboardKeyTester
    {
        public KeyboardKeyTester(TestDevice device, AhkKey key, bool block = false)
        {
            Console.WriteLine($"Test key: {key.Name} - code {key.LogCode()}");
            var im = new Manager();

            var devId = device.GetDeviceId();

            if (devId == 0) return;

            im.SubscribeKey(devId, 0x2, block, new Action<int>(OnKeyEvent));
        }

        public void OnKeyEvent(int value)
        {
            Console.WriteLine($"State: {value}");
        }

    }
}
