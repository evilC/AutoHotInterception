using System;
using System.Collections.Concurrent;
using System.Threading;
using AutoHotInterception.Helpers;
using static AutoHotInterception.Helpers.HelperFunctions;

namespace AutoHotInterception
{
    public class Monitor : IDisposable
    {
        private readonly IntPtr _deviceContext;

        private readonly ConcurrentDictionary<int, bool> _filteredDevices = new ConcurrentDictionary<int, bool>();

        private dynamic _keyboardCallback;
        private dynamic _mouseCallback;

        private Thread _pollThread;
        private volatile bool _pollThreadRunning;

        #region Public

        public Monitor()
        {
            _deviceContext = ManagedWrapper.CreateContext();
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

        public DeviceInfo[] GetDeviceList()
        {
            return HelperFunctions.GetDeviceList(_deviceContext);
        }

        #endregion

        #region Private

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
                _pollThreadRunning = true;
                _pollThread.Join();
                _pollThread = null;
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

        private void PollThread()
        {
            var stroke = new ManagedWrapper.Stroke();

            while (_pollThreadRunning)
            {
                for (var i = 1; i < 11; i++)
                    while (ManagedWrapper.Receive(_deviceContext, i, ref stroke, 1) > 0)
                    {
                        ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
                        FireKeyboardCallback(i, stroke);
                    }

                for (var i = 11; i < 21; i++)
                    while (ManagedWrapper.Receive(_deviceContext, i, ref stroke, 1) > 0)
                    {
                        ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
                        FireMouseCallback(i, stroke);
                    }

                Thread.Sleep(10);
            }
        }

        private void FireKeyboardCallback(int id, ManagedWrapper.Stroke stroke)
        {
            ThreadPool.QueueUserWorkItem(threadProc =>
                _keyboardCallback(id, stroke.key.state, stroke.key.code, stroke.key.information));
        }

        private void FireMouseCallback(int id, ManagedWrapper.Stroke stroke)
        {
            ThreadPool.QueueUserWorkItem(threadProc => _mouseCallback(id, stroke.mouse.state, stroke.mouse.flags,
                stroke.mouse.rolling, stroke.mouse.x, stroke.mouse.y, stroke.mouse.information));
        }

        #endregion
    }
}