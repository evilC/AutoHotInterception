using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoHotInterception.Helpers;
using static AutoHotInterception.Helpers.HelperFunctions;

namespace AutoHotInterception
{
    public class Monitor : IDisposable
    {
        private readonly IntPtr _deviceContext;
        private Thread _pollThread;
        private bool _pollThreadRunning = false;
        private dynamic _keyboardCallback;
        private dynamic _mouseCallback;
        private bool _filterState = false;
        private readonly ConcurrentDictionary<int, bool> _filteredDevices = new ConcurrentDictionary<int, bool>();

        public Monitor()
        {
            _deviceContext = ManagedWrapper.CreateContext();
            SetThreadState(true);
        }

        public string OkCheck()
        {
            return "OK";
        }

        //public void Log(string text)
        //{
        //    Debug.WriteLine($"AHK| {text}");
        //}

        public void Subscribe(dynamic keyboardCallback, dynamic mouseCallback)
        {
            _keyboardCallback = keyboardCallback;
            _mouseCallback = mouseCallback;
        }

        public bool SetDeviceFilterState(int device, bool state)
        {
            SetFilterState(false);
            if (state)
            {
                _filteredDevices[device] = true;
                //Log($"Adding device {device}, count: {_filteredDevices.Count}");
            }
            else
            {
                _filteredDevices.TryRemove(device, out _);
                //Log($"Removing device {device}, count: {_filteredDevices.Count}");
            }

            if (_filteredDevices.Count > 0)
            {
                SetFilterState(true);
            }
            return true;
        }

        public DeviceInfo[] GetDeviceList()
        {
            return HelperFunctions.GetDeviceList(_deviceContext);
        }

        private void SetFilterState(bool state)
        {
            ManagedWrapper.SetFilter(_deviceContext, IsMonitoredDevice,
                state ? ManagedWrapper.Filter.All : ManagedWrapper.Filter.None);
            _filterState = state;
        }

        private int IsMonitoredDevice(int device)
        {
            return Convert.ToInt32(_filteredDevices.ContainsKey(device));
        }


        private void SetThreadState(bool state)
        {
            if (state)
            {
                if (_pollThreadRunning) return;

                _pollThreadRunning = true;
                _pollThread = new Thread(PollThread);
                _pollThread.Start();
            }
            else
            {
                _pollThread.Abort();
                _pollThread.Join();
                _pollThread = null;
            }
        }

        private void PollThread()
        {
            var stroke = new ManagedWrapper.Stroke();

            while (true)
            {
                for (var i = 1; i < 11; i++)
                {
                    while (ManagedWrapper.Receive(_deviceContext, i, ref stroke, 1) > 0)
                    {
                        ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
                        var processedState = KeyboardStrokeToKeyboardState(stroke);
                        var info = "";
                        if (processedState.Ignore)
                        {
                            FireKeyboardCallback(i, new KeyboardCallback
                            {
                                Id = i,
                                Code = stroke.key.code,
                                State = stroke.key.state,
                                Info = "Ignored - showing raw values"
                            });
                        }
                        else
                        {
                            FireKeyboardCallback(i, new KeyboardCallback
                            {
                                Id = i,
                                Code = processedState.Code,
                                State = processedState.State,
                                Info = stroke.key.code > 255 ? "Extended" : ""
                            });
                        }
                    }
                }

                for (var i = 11; i < 21; i++)
                {
                    while (ManagedWrapper.Receive(_deviceContext, i, ref stroke, 1) > 0)
                    {
                        ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
                        if (stroke.mouse.state != 0)
                        {
                            // Mouse Button
                            var btnState = MouseStrokeToButtonState(stroke);
                            FireMouseCallback(new MouseCallback
                            {
                                Id = i,
                                Code = btnState.Button,
                                State = btnState.State,
                                Info = "Mouse Button"
                            });
                        }
                        else if ((stroke.mouse.flags & (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute) ==
                                 (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute)
                        {
                            // Absolute Mouse Move
                            FireMouseCallback(new MouseCallback
                            {
                                Id = i,
                                X = stroke.mouse.x,
                                Y = stroke.mouse.y,
                                Info = "Absolute Move"
                            });

                        }
                        else if ((stroke.mouse.flags & (ushort)ManagedWrapper.MouseFlag.MouseMoveRelative) ==
                                 (ushort)ManagedWrapper.MouseFlag.MouseMoveRelative)

                        {
                            // Relative Mouse Move
                            FireMouseCallback(new MouseCallback
                            {
                                Id = i,
                                X = stroke.mouse.x,
                                Y = stroke.mouse.y,
                                Info = "Relative Move"
                            });

                        }
                        //FireMouseCallback(i, stroke);
                    }
                }
                Thread.Sleep(10);
            }
        }

        private void FireKeyboardCallback(int id, KeyboardCallback data)
        {
            ThreadPool.QueueUserWorkItem(threadProc => _keyboardCallback(data.Id, data.Code, data.State, data.Info));
        }

        private void FireMouseCallback(MouseCallback data)
        {
            ThreadPool.QueueUserWorkItem(threadProc => _mouseCallback(data.Id, data.Code, data.State, data.X, data.Y, data.Info));
        }

        public class MouseCallback
        {
            public int Id { get; set; }
            public int Code { get; set; }
            public int State { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public string Info { get; set; } = "";
        }

        public class KeyboardCallback
        {
            public int Id { get; set; }
            public int Code { get; set; }
            public int State { get; set; }
            public string Info { get; set; } = "";
        }

        public void Dispose()
        {
            SetFilterState(false);
            SetThreadState(false);
        }
    }
}
