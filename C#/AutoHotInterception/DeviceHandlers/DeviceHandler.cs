using AutoHotInterception.Helpers;
using System;

namespace AutoHotInterception.DeviceHandlers
{
    abstract class DeviceHandler : IDeviceHandler
    {
        protected IntPtr DeviceContext;
        protected int _deviceId;

        public DeviceHandler(IntPtr deviceContext, int deviceId)
        {
            DeviceContext = deviceContext;
            _deviceId = deviceId;
        }

        public abstract void ProcessStroke(ManagedWrapper.Stroke stroke);
    }
}
