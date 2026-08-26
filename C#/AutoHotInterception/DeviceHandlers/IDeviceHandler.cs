using AutoHotInterception.Helpers;
using System.Collections.Generic;

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
        /// Subscribes to a single key or button of this device
        /// </summary>
        /// <param name="code">The ScanCode (keyboard) or Button Code (mouse) for the key or button</param>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        void SubscribeSingleButton(ushort code, MappingOptions mappingOptions);

        /// <summary>
        /// Subscribes to a single key or button of this device
        /// </summary>
        /// <param name="code">The ScanCode (keyboard) or Button Code (mouse) for the key or button</param>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        void SubscribeSingleButtonEx(ushort code, MappingOptions mappingOptions);

        /// <summary>
        /// Unsubscribes from a single key or button of this device
        /// </summary>
        /// <param name="code">The ScanCode (keyboard) or Button Code (mouse) for the key or button</param>
        void UnsubscribeSingleButton(ushort code);

        /// <summary>
        /// Subscribes to all keys or buttons of this device
        /// </summary>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        void SubscribeAllButtons(MappingOptions mappingOptions);

        /// <summary>
        /// Unsubscribes from a SubscribeAll for this device
        /// </summary>
        void UnsubscribeAllButtons();

        /// <summary>
        /// Enables Context Mode for this device
        /// </summary>
        /// <param name="callback">The callback to call when input happens</param>
        void SetContextCallback(dynamic callback);

        /// <summary>
        /// Removes Context Mode for this device
        /// </summary>
        void RemoveContextCallback();

        /// <summary>
        /// After doing an UnsubscribeButton, UnsubscribeAll, or RemoveContext, this method will be called
        /// If no more subscriptions are present, it should remove the filter for this device
        /// </summary>
        void DisableFilterIfNeeded();

        /// <summary>
        /// Process an incoming stroke, or a pair of extended keycode strokes
        /// </summary>
        /// <param name="strokes">The stroke(s) to process</param>
        void ProcessStroke(List<ManagedWrapper.Stroke> strokes);
    }
}
