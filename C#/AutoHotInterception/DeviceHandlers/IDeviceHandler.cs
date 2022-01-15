using AutoHotInterception.Helpers;

namespace AutoHotInterception.DeviceHandlers
{
    interface IDeviceHandler
    {
        void ProcessStroke(ManagedWrapper.Stroke stroke);
    }
}
