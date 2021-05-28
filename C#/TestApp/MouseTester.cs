using System;
using System.Threading;
using AutoHotInterception;

namespace TestApp
{
    public class MouseTester
    {
        private bool _subscribed = false;
        private bool _state = true;
        private const string MouseHandle = "HID\\VID_046D&PID_C00C&REV_0620"; // Logitech USB
        //private const string MouseHandle = @"HID\VID_045E&PID_00D1&REV_0120&Col02"; // MS Mouse
        private readonly Manager _im = new Manager();
        private readonly int _devId;
        private int _counter;

        public MouseTester()
        {
            _devId = _im.GetMouseIdFromHandle(MouseHandle);

            if (_devId == 0) return;
            Console.WriteLine("Hit S to unsubscribe / subscribe");
            Console.WriteLine("Hit T to enable / disable state");

            SetSubscribeState(true);

            while (true)
            {
                while (Console.KeyAvailable == false)
                    Thread.Sleep(250);
                var cki = Console.ReadKey(true);
                switch (cki.Key)
                {
                    case ConsoleKey.S:
                        SetSubscribeState(!_subscribed);
                        break;

                    case ConsoleKey.T:
						Console.WriteLine(_state ? "Disabling" : "Enabling");
                        _im.SetState(!_state);
                        Console.WriteLine(_state ? "Disabled" : "Enabled");
                        _state = !_state;
                        break;
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
