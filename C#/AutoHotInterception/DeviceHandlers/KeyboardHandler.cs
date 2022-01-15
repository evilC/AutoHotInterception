using AutoHotInterception.Helpers;
using System;

namespace AutoHotInterception.DeviceHandlers
{
    class KeyboardHandler : DeviceHandler
    {
        public KeyboardHandler(IntPtr deviceContext, int deviceId) : base (deviceContext, deviceId)
        {

        }

        public override void ProcessStroke(ManagedWrapper.Stroke stroke)
        {
            ManagedWrapper.Send(DeviceContext, _deviceId, ref stroke, 1);
        }
    }
}
