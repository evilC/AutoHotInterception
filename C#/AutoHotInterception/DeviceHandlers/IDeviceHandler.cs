using AutoHotInterception.Helpers;

namespace AutoHotInterception.DeviceHandlers
{
    interface IDeviceHandler
    {
        /// <summary>
        /// Will be called by Interception to decide whether or not to filter (consume input) from this device
        /// A "predicate" function is passed to Interception's SetFilter method, and it expects a 0 (Not Filtered) or 1 (Filtered) result
        /// </summary>
        /// <returns>0 for not filtered, 1 for filtered</returns>
        int IsFiltered();

        /// <summary>
        /// Lets this device know if it is currently being filtered or not, and governs what IsFiltered() returns
        /// </summary>
        /// <param name="state">true for filtered, false for not filtered</param>
        void SetFilterState(bool state);

        /// <summary>
        /// Process an incoming stroke
        /// </summary>
        /// <param name="stroke">The stroke to process</param>
        void ProcessStroke(ManagedWrapper.Stroke stroke);
    }
}
