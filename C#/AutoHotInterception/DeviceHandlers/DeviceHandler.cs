using AutoHotInterception.Helpers;
using System;
using System.Collections.Concurrent;

namespace AutoHotInterception.DeviceHandlers
{
    abstract class DeviceHandler : IDeviceHandler
    {
        protected dynamic ContextCallback;

        protected IntPtr DeviceContext;
        protected int DeviceId;
        public bool IsFiltered { get; set; }

        // Holds MappingOptions for individual mouse button / keyboard key subscriptions
        protected ConcurrentDictionary<ushort, MappingOptions> SingleButtonMappings = new ConcurrentDictionary<ushort, MappingOptions>();
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
                WorkerThreads[code].Start();
            }
            IsFiltered = true;
        }

        /// <summary>
        /// Unsubscribes from a single key or button of this device
        /// </summary>
        /// <param name="code">The ScanCode (keyboard) or Button Code (mouse) for the key or button</param>
        public void UnsubscribeSingleButton(ushort code)
        {
            SingleButtonMappings.TryRemove(code, out var mappingOptions);
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
                DeviceWorkerThread.Start();
            }
            IsFiltered = true;
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
            IsFiltered = true;
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
        int IDeviceHandler.IsFiltered()
        {
            return Convert.ToInt32(IsFiltered);
        }

        /// <summary>
        /// Lets this device know if it is currently being filtered or not, and governs what IsFiltered() returns
        /// </summary>
        /// <param name="state">true for filtered, false for not filtered</param>
        public void SetFilterState(bool state)
        {
            IsFiltered = state;
        }

        /// <summary>
        /// After doing an UnsubscribeButton, UnsubscribeAll, or RemoveContext, this method will be called
        /// If no more subscriptions are present, it should remove the filter for this device
        /// </summary>
        public abstract void DisableFilterIfNeeded();

        /// <summary>
        /// Process an incoming stroke
        /// </summary>
        /// <param name="stroke">The stroke to process</param>
        public abstract void ProcessStroke(ManagedWrapper.Stroke stroke);
    }
}
