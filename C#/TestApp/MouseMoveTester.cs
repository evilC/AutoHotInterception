using System;
using System.Threading;
using AutoHotInterception;

namespace TestApp
{
    public class MouseMoveTester
    {
        private bool _subscribed = false;
        private readonly Manager _im = new Manager();
        private readonly int _devId;
        private int _counter;

        public MouseMoveTester(TestDevice device)
        {
            _devId = device.GetDeviceId();

            if (_devId == 0) return;
            Console.WriteLine("Hit S to unsubscribe / subscribe");

            SetSubscribeState(true);

            while (true)
            {
                while (Console.KeyAvailable == false)
                    Thread.Sleep(250);
                var cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.S)
                {
                    SetSubscribeState(!_subscribed);
                }
            }
        }

        private void SetSubscribeState(bool state)
        {
            if (state && !_subscribed)
            {
                Console.WriteLine("Subscribing...");
                _subscribed = true;
                _im.SubscribeMouseButton(_devId, 1, true, new Action<int>(value =>
                {
                    Console.WriteLine("RButton Button Value: " + value);
                }));
                _im.SubscribeMouseButton(_devId, 3, true, new Action<int>(value =>
                {
                    Console.WriteLine("XButton1 Button Value: " + value);
                }));
                _im.SubscribeMouseButton(_devId, 4, true, new Action<int>(value =>
                {
                    Console.WriteLine("XButton2 Button Value: " + value);
                }));
                _im.SubscribeMouseButton(_devId, 5, true, new Action<int>(value =>
                {
                    Console.Write("WheelVertical Value: " + value);
                    var mycounter = _counter;
                    mycounter++;
                    Console.WriteLine(" Counter: " + mycounter);
                    _counter = mycounter;
                }));
                _im.SubscribeMouseMove(_devId, true, new Action<int, int>((x, y) =>
                {
                    Console.WriteLine($"Mouse Move: x: {x}, y: {y}");
                }));
            }
            else if (!state && _subscribed)
            {
                _subscribed = false;
                Console.WriteLine("Unsubscribing...");
                _im.UnsubscribeMouseButtons(_devId);
                _im.UnsubscribeMouseMove(_devId);

            }
        }
    }
}
