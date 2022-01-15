using System;
using AutoHotInterception.Helpers;

namespace AutoHotInterception.DeviceHandlers
{
    class MouseHandler : DeviceHandler
    {
        public MouseHandler(IntPtr deviceContext, int deviceId) : base(deviceContext, deviceId)
        {

        }

        public override void ProcessStroke(ManagedWrapper.Stroke stroke)
        {
            throw new NotImplementedException();
        }
    }
}
