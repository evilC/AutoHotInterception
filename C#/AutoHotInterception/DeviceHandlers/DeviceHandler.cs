using AutoHotInterception.Helpers;
using System;
using System.Collections.Concurrent;

namespace AutoHotInterception.DeviceHandlers
{
    abstract class DeviceHandler : IDeviceHandler
    {
        protected IntPtr DeviceContext;
        protected int _deviceId;
        public bool IsFiltered { get; set; }

        protected readonly ConcurrentDictionary<ushort, WorkerThread> WorkerThreads = new ConcurrentDictionary<ushort, WorkerThread>();
        protected WorkerThread DeviceWorkerThread;

        public DeviceHandler(IntPtr deviceContext, int deviceId)
        {
            DeviceContext = deviceContext;
            _deviceId = deviceId;
        }


        public abstract void ProcessStroke(ManagedWrapper.Stroke stroke);

        int IDeviceHandler.IsFiltered()
        {
            return Convert.ToInt32(IsFiltered);
        }

        public void SetFilterState(bool state)
        {
            IsFiltered = state;
        }
    }
}
