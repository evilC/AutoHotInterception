using AutoHotInterception.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AutoHotInterception.DeviceHandlers
{
    abstract class DeviceHandler : IDeviceHandler
    {
        protected dynamic ContextCallback;

        protected IntPtr DeviceContext;
        protected int DeviceId;
        protected bool _isFiltered { get; set; }

        // Holds MappingOptions for individual mouse button / keyboard key subscriptions
        protected ConcurrentDictionary<ushort, MappingOptions> SingleButtonMappings = new ConcurrentDictionary<ushort, MappingOptions>();
        protected ConcurrentDictionary<ushort, MappingOptions> SingleButtonMappingsEx = new ConcurrentDictionary<ushort, MappingOptions>();
        // If all mouse buttons or keyboard keys are subscribed, this holds the mapping options
        protected MappingOptions AllButtonsMapping;

        protected readonly ConcurrentDictionary<ushort, WorkerThread> WorkerThreads = new ConcurrentDictionary<ushort, WorkerThread>();
        protected WorkerThread DeviceWorkerThread;

        public DeviceHandler(IntPtr deviceContext, int deviceId)
        {
            DeviceContext = deviceContext;
            DeviceId = deviceId;
        }

        /// <summary>
        /// Subscribes to a single key or button of this device
        /// </summary>
        /// <param name="code">The ScanCode (keyboard) or Button Code (mouse) for the key or button</param>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        public void SubscribeSingleButton(ushort code, MappingOptions mappingOptions)
        {
            SingleButtonMappings.TryAdd(code, mappingOptions);
            if (!mappingOptions.Concurrent && !WorkerThreads.ContainsKey(code))
            {
                WorkerThreads.TryAdd(code, new WorkerThread());
            }
            _isFiltered = true;
        }

        /// <summary>
        /// Subscribes to a single key or button of this device
        /// </summary>
        /// <param name="code">The ScanCode (keyboard) or Button Code (mouse) for the key or button</param>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        public void SubscribeSingleButtonEx(ushort code, MappingOptions mappingOptions)
        {
            SingleButtonMappingsEx.TryAdd(code, mappingOptions);
            if (!mappingOptions.Concurrent && !WorkerThreads.ContainsKey(code))
            {
                WorkerThreads.TryAdd(code, new WorkerThread());
            }
            _isFiltered = true;
        }

        /// <summary>
        /// Unsubscribes from a single key or button of this device
        /// </summary>
        /// <param name="code">The ScanCode (keyboard) or Button Code (mouse) for the key or button</param>
        public void UnsubscribeSingleButton(ushort code)
        {
            if (!SingleButtonMappings.ContainsKey(code)) return;
            SingleButtonMappings.TryRemove(code, out var mappingOptions);
            if (!mappingOptions.Concurrent && WorkerThreads.ContainsKey(code))
            {
                WorkerThreads[code].Dispose();
                WorkerThreads.TryRemove(code, out _);
            }
            DisableFilterIfNeeded();
        }

        /// <summary>
        /// Unsubscribes from a single key or button of this device
        /// </summary>
        /// <param name="code">The ScanCode (keyboard) or Button Code (mouse) for the key or button</param>
        public void UnsubscribeSingleButtonEx(ushort code)
        {
            if (!SingleButtonMappingsEx.ContainsKey(code)) return;
            SingleButtonMappingsEx.TryRemove(code, out var mappingOptions);
            if (!mappingOptions.Concurrent && WorkerThreads.ContainsKey(code))
            {
                WorkerThreads[code].Dispose();
                WorkerThreads.TryRemove(code, out _);
            }
            DisableFilterIfNeeded();
        }

        /// <summary>
        /// Subscribes to all keys or buttons of this device
        /// </summary>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        public void SubscribeAllButtons(MappingOptions mappingOptions)
        {
            AllButtonsMapping = mappingOptions;
            if (!mappingOptions.Concurrent && DeviceWorkerThread == null)
            {
                DeviceWorkerThread = new WorkerThread();
            }
            _isFiltered = true;
        }

        /// <summary>
        /// Unsubscribes from a SubscribeAll for this device
        /// </summary>
        public void UnsubscribeAllButtons()
        {
            if (AllButtonsMapping == null) return;
            // Stop DeviceWorkerThread
            if (!AllButtonsMapping.Concurrent && DeviceWorkerThread != null)
            {
                DeviceWorkerThread.Dispose();
                DeviceWorkerThread = null;
            }
            AllButtonsMapping = null;
            DisableFilterIfNeeded();
        }

        /// <summary>
        /// Enables Context Mode for this device
        /// </summary>
        /// <param name="callback">The callback to call when input happens</param>
        public void SetContextCallback(dynamic callback)
        {
            ContextCallback = callback;
            _isFiltered = true;
        }

        /// <summary>
        /// Removes Context Mode for this device
        /// </summary>
        public void RemoveContextCallback()
        {
            ContextCallback = null;
            DisableFilterIfNeeded();
        }

        // ToDo: Why is this IDeviceHandler.IsFiltered() and other Interface methods aren't?
        public int IsFiltered()
        {
            return Convert.ToInt32(_isFiltered);
        }

        /// <summary>
        /// After doing an UnsubscribeButton, UnsubscribeAll, or RemoveContext, this method will be called
        /// If no more subscriptions are present, it should remove the filter for this device
        /// </summary>
        public abstract void DisableFilterIfNeeded();

        /// <summary>
        /// Process an incoming stroke, or a pair of extended keycode strokes
        /// </summary>
        /// <param name="strokes">The stroke(s) to process</param>
        public abstract void ProcessStroke(List<ManagedWrapper.Stroke> strokes);
    }
}
