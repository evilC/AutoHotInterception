using System;
using System.Collections.Concurrent;
using System.Threading;
using AutoHotInterception.Helpers;

namespace AutoHotInterception
{
    public class Monitor : IDisposable
    {
        private readonly IntPtr _deviceContext;

        private readonly ConcurrentDictionary<int, bool> _filteredDevices = new ConcurrentDictionary<int, bool>();

        private dynamic _keyboardCallback;
        private dynamic _mouseCallback;

        private readonly MultimediaTimer _timer;
        private readonly int _pollRate = 1;
        private volatile bool _pollThreadRunning;

        #region Public

        public Monitor()
        {
            _deviceContext = ManagedWrapper.CreateContext();
            _timer = new MultimediaTimer() { Interval = _pollRate };
            _timer.Elapsed += DoPoll;
            SetThreadState(true);
        }

        public void Dispose()
        {
            SetFilterState(false);
            SetThreadState(false);
        }

        public string OkCheck()
        {
            return "OK";
        }

        public void Subscribe(dynamic keyboardCallback, dynamic mouseCallback)
        {
            _keyboardCallback = keyboardCallback;
            _mouseCallback = mouseCallback;
        }

        public bool SetDeviceFilterState(int device, bool state)
        {
            SetFilterState(false);
            if (state)
                _filteredDevices[device] = true;
            else
                _filteredDevices.TryRemove(device, out _);

            if (_filteredDevices.Count > 0) SetFilterState(true);
            return true;
        }

        public HelperFunctions.DeviceInfo[] GetDeviceList()
        {
            return HelperFunctions.GetDeviceList(_deviceContext);
        }

        public int GetKeyboardId(int vid, int pid, int instance = 1)
        {
            return GetDeviceId(false, vid, pid, instance);
        }

        public int GetMouseId(int vid, int pid, int instance = 1)
        {
            return GetDeviceId(true, vid, pid, instance);
        }

        public int GetKeyboardIdFromHandle(string handle, int instance = 1)
        {
            return HelperFunctions.GetDeviceIdFromHandle(_deviceContext, false, handle, instance);
        }

        public int GetMouseIdFromHandle(string handle, int instance = 1)
        {
            return HelperFunctions.GetDeviceIdFromHandle(_deviceContext, true, handle, instance);
        }

        public int GetDeviceIdFromHandle(bool isMouse, string handle, int instance = 1)
        {
            return HelperFunctions.GetDeviceIdFromHandle(_deviceContext, isMouse, handle, instance);
        }

        public int GetDeviceId(bool isMouse, int vid, int pid, int instance = 1)
        {
            return HelperFunctions.GetDeviceId(_deviceContext, isMouse, vid, pid, instance);
        }

        #endregion

        #region Private

        private void SetThreadState(bool state)
        {
            if (state && !_timer.IsRunning)
            {
                SetFilterState(true);
                _timer.Start();
            }
            else if (!state && _timer.IsRunning)
            {
                SetFilterState(false);
                _timer.Stop();
                while (_pollThreadRunning) // Are we mid-poll?
                {
                    Thread.Sleep(10); // Wait until poll ends
                }
            }
        }

        private int IsMonitoredDevice(int device)
        {
            return Convert.ToInt32(_filteredDevices.ContainsKey(device));
        }

        private void SetFilterState(bool state)
        {
            ManagedWrapper.SetFilter(_deviceContext, IsMonitoredDevice,
                state ? ManagedWrapper.Filter.All : ManagedWrapper.Filter.None);
        }

        private void DoPoll(object sender, EventArgs e)
        {
            _pollThreadRunning = true;

            var stroke = new ManagedWrapper.Stroke();

            for (var i = 1; i < 11; i++)
            {
                while (ManagedWrapper.Receive(_deviceContext, i, ref stroke, 1) > 0)
                {
                    ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
                    var processedState = HelperFunctions.KeyboardStrokeToKeyboardState(stroke);
                    if (processedState.Ignore)
                        FireKeyboardCallback(i, new KeyboardCallback
                        {
                            Id = i,
                            Code = stroke.key.code,
                            State = stroke.key.state,
                            Info = "Ignored - showing raw values"
                        });
                    else
                        FireKeyboardCallback(i, new KeyboardCallback
                        {
                            Id = i,
                            Code = processedState.Code,
                            State = processedState.State,
                            Info = stroke.key.code > 255 ? "Extended" : ""
                        });
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
                        var btnStates = HelperFunctions.MouseStrokeToButtonStates(stroke);
                        foreach (var btnState in btnStates)
                        {
                            FireMouseCallback(new MouseCallback
                            {
                                Id = i,
                                Code = btnState.Button,
                                State = btnState.State,
                                Info = "Mouse Button"
                            });
                        }
                    }
                    else if ((stroke.mouse.flags & (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute) ==
                             (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute)
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
                }
            }


            _pollThreadRunning = false;
        }

        private void FireKeyboardCallback(int id, KeyboardCallback data)
        {
            ThreadPool.QueueUserWorkItem(threadProc =>
                _keyboardCallback(data.Id, data.Code, data.State, data.Info));
        }

        private void FireMouseCallback(MouseCallback data)
        {
            ThreadPool.QueueUserWorkItem(threadProc =>
                _mouseCallback(data.Id, data.Code, data.State, data.X, data.Y, data.Info));
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

        #endregion
    }
}