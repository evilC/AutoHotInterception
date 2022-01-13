using AutoHotInterception;
using System;

namespace TestApp
{
    class SetStateTester
    {
        public SetStateTester(TestDevice device, AhkKey key)
        {
            Console.WriteLine($"Test key: {key.Name} - code");
            Console.WriteLine("Enter to toggle Subscription state");
            var im = new Manager();
            var devId = device.GetDeviceId();
            if (devId == 0) return;
            var state = true;

            im.SubscribeKey(devId, key.Code, true, new Action<int>(OnKeyEvent));

            while (true)
            {
                Console.ReadLine();
                state = !state;
                Console.WriteLine($"SetState({state})");
                im.SetState(state);
            }
        }

        public void OnKeyEvent(int value)
        {
            Console.WriteLine($"State: {value}");
        }
    }
}
