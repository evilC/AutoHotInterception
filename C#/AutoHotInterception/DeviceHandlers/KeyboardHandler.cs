using AutoHotInterception.Helpers;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AutoHotInterception.DeviceHandlers
{
    class KeyboardHandler : DeviceHandler
    {
        private ConcurrentDictionary<ushort, MappingOptions> KeyboardKeyMappings = new ConcurrentDictionary<ushort, MappingOptions>();
        private MappingOptions KeyboardMapping;
        dynamic ContextCallback;

        public KeyboardHandler(IntPtr deviceContext, int deviceId) : base (deviceContext, deviceId)
        {
            
        }

        /// <summary>
        /// Called when we are removing a Subscription or Context Mode
        /// If there are no other subscriptions, and Context Mode is disabled, turn the filter off
        /// </summary>
        private void DisableFilterIfNeeded()
        {
            if (KeyboardMapping != null || KeyboardKeyMappings.Count > 0 || ContextCallback != null)
            {
                IsFiltered = false;
            }
        }

        /// <summary>
        /// Subscribe to a specific key on this keyboard
        /// </summary>
        /// <param name="code">The ScanCode of the key to subscribe to</param>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        public void SubscribeKey(ushort code, MappingOptions mappingOptions)
        {
            KeyboardKeyMappings.TryAdd(code, mappingOptions);
            if (!mappingOptions.Concurrent)
            {
                WorkerThreads.TryAdd(code, new WorkerThread());
                WorkerThreads[code].Start();
            }
            IsFiltered = true;
        }

        /// <summary>
        /// Unsubscribe from a specific key on this keyboard
        /// </summary>
        /// <param name="code">The ScanCode of the key to subscribe to</param>
        public void UnsubscribeKey(ushort code)
        {
            KeyboardKeyMappings.TryRemove(code, out _);
            DisableFilterIfNeeded();
        }

        /// <summary>
        /// Subscribe to all keys on this keyboard
        /// </summary>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        public void SubscribeKeyboard(MappingOptions mappingOptions)
        {
            KeyboardMapping = mappingOptions;
            if (!mappingOptions.Concurrent)
            {
                if (DeviceWorkerThread == null)
                {
                    DeviceWorkerThread = new WorkerThread();
                    DeviceWorkerThread.Start();
                }
            }
            IsFiltered = true;
        }

        public void UnsubscribeKeyboard()
        {
            if (KeyboardMapping == null) return;
            // Stop DeviceWorkerThread
            if (!KeyboardMapping.Concurrent)
            {
                if (DeviceWorkerThread != null)
                {
                    DeviceWorkerThread.Dispose();
                }
            }
            KeyboardMapping = null;
            DisableFilterIfNeeded();
        }

        /// <summary>
        /// Enables Context Mode for this keyboard
        /// </summary>
        /// <param name="callback">The callback to call when input happens</param>
        public void SetContextCallback(dynamic callback)
        {
            ContextCallback = callback;
            IsFiltered = true;
        }

        public void RemoveContextCallback()
        {
            ContextCallback = null;
            DisableFilterIfNeeded();
        }

        public override void ProcessStroke(ManagedWrapper.Stroke stroke)
        {
            //ManagedWrapper.Send(DeviceContext, _deviceId, ref stroke, 1);
            var hasSubscription = false;
            var hasContext = ContextCallback != null;

            // Process any waiting input for this keyboard
            var block = false;

            if (IsFiltered)
            {
                var isKeyMapping = false; // True if this is a mapping to a single key, else it would be a mapping to a whole device
                var processedState = HelperFunctions.KeyboardStrokeToKeyboardState(stroke);
                var code = processedState.Code;
                var state = processedState.State;
                MappingOptions mapping = null;

                // If there is a mapping to this specific key, then use that ...
                if (KeyboardKeyMappings.ContainsKey(code))
                {
                    isKeyMapping = true;
                    mapping = KeyboardKeyMappings[code];
                }
                // ... otherwise, if there is a mapping to the whole keyboard, use that
                else if (KeyboardMapping != null)
                {
                    mapping = KeyboardMapping;
                }

                if (mapping != null)
                {
                    // Begin translation of incoming key code, state, extended flag etc...
                    var processMappings = true;
                    if (processedState.Ignore)
                    {
                        // Set flag to stop Context Mode from firing
                        hasSubscription = true;
                        // Set flag to indicate disable mapping processing
                        processMappings = false;
                    }
                    if (processMappings)
                    {
                        hasSubscription = true;

                        if (mapping.Block) block = true;
                        if (mapping.Concurrent)
                        {
                            if (isKeyMapping)
                            {
                                ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(state));
                            }
                            else
                            {
                                ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(code, state));
                            }
                        }
                        else
                        {
                            if (isKeyMapping)
                            {
                                WorkerThreads[code]?.Actions.Add(() => mapping.Callback(state));
                            }
                            else
                            {
                                DeviceWorkerThread?.Actions.Add(() => mapping.Callback(code, state));
                            }
                        }
                    }

                }
                // If the key was blocked by Subscription Mode, then move on to next key...
                if (block) return;

                // If this key had no subscriptions, but Context Mode is set for this keyboard...
                // ... then set the Context before sending the key
                if (!hasSubscription && hasContext) ContextCallback(1);

                // Pass the key through to the OS.
                ManagedWrapper.Send(DeviceContext, _deviceId, ref stroke, 1);

                // If we are processing Context Mode, then Unset the context variable after sending the key
                if (!hasSubscription && hasContext) ContextCallback(0);
            }
        }
    }
}
