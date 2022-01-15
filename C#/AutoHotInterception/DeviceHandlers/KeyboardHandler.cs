using AutoHotInterception.Helpers;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AutoHotInterception.DeviceHandlers
{
    class KeyboardHandler : DeviceHandler
    {
        private ConcurrentDictionary<ushort, MappingOptions> KeyboardKeyMappings = new ConcurrentDictionary<ushort, MappingOptions>();

        public KeyboardHandler(IntPtr deviceContext, int deviceId) : base (deviceContext, deviceId)
        {
            
        }

        public override void ProcessStroke(ManagedWrapper.Stroke stroke)
        {
            //ManagedWrapper.Send(DeviceContext, _deviceId, ref stroke, 1);
            var hasSubscription = false;
            //var hasContext = ContextCallbacks.ContainsKey(i);
            var hasContext = false;

            // Process any waiting input for this keyboard
            var block = false;

            if (IsFiltered)
            {
                var isKeyMapping = false; // True if this is a mapping to a single key, else it would be a mapping to a whole device
                var processedState = HelperFunctions.KeyboardStrokeToKeyboardState(stroke);
                var code = processedState.Code;
                var state = processedState.State;
                MappingOptions mapping = null;

                if (KeyboardKeyMappings.ContainsKey(code))
                {
                    isKeyMapping = true;
                    mapping = KeyboardKeyMappings[code];
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
                                //DeviceWorkerThreads[i]?.Actions.Add(() => mapping.Callback(code, state));
                            }
                        }
                    }

                }
                // If the key was blocked by Subscription Mode, then move on to next key...
                if (block) return;

                // If this key had no subscriptions, but Context Mode is set for this keyboard...
                // ... then set the Context before sending the key
                //if (!hasSubscription && hasContext) ContextCallbacks[i](1);

                // Pass the key through to the OS.
                ManagedWrapper.Send(DeviceContext, _deviceId, ref stroke, 1);

                // If we are processing Context Mode, then Unset the context variable after sending the key
                //if (!hasSubscription && hasContext) ContextCallbacks[i](0);
            }
        }

        public void SubscribeKey(ushort code, MappingOptions mappingOptions)
        {
            KeyboardKeyMappings.TryAdd(code, mappingOptions);
            if (!mappingOptions.Concurrent)
            {
                WorkerThreads.TryAdd(code, new WorkerThread());
                WorkerThreads[code].Start();
            }
        }
    }
}
