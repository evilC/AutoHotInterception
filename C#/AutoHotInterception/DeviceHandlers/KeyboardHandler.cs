using AutoHotInterception.Helpers;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AutoHotInterception.DeviceHandlers
{
    class KeyboardHandler : DeviceHandler
    {
        public KeyboardHandler(IntPtr deviceContext, int deviceId) : base (deviceContext, deviceId)
        {
            
        }

        /// <summary>
        /// Called when we are removing a Subscription or Context Mode
        /// If there are no other subscriptions, and Context Mode is disabled, turn the filter off
        /// </summary>
        public override void DisableFilterIfNeeded()
        {
            if (AllButtonsMapping == null 
                && SingleButtonMappings.Count == 0
                && ContextCallback == null)
            {
                IsFiltered = false;
            }
        }

        // ScanCode notes: https://www.win.tue.nl/~aeb/linux/kbd/scancodes-1.html
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
                if (SingleButtonMappings.ContainsKey(code))
                {
                    isKeyMapping = true;
                    mapping = SingleButtonMappings[code];
                }
                // ... otherwise, if there is a mapping to the whole keyboard, use that
                else if (AllButtonsMapping != null)
                {
                    mapping = AllButtonsMapping;
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
                ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);

                // If we are processing Context Mode, then Unset the context variable after sending the key
                if (!hasSubscription && hasContext) ContextCallback(0);
            }
        }
    }
}
