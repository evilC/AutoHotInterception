using AutoHotInterception.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                _isFiltered = false;
            }
        }

        #region Input Synthesis
        /// <summary>
        /// Sends a keyboard key event
        /// </summary>
        /// <param name="code">The ScanCode to send</param>
        /// <param name="state">The State to send (1 = pressed, 0 = released)</param>
        public void SendKeyEvent(ushort code, int state)
        {
            var st = 1 - state;
            var stroke = new ManagedWrapper.Stroke();
            if (code > 255)
            {
                code -= 256;
                if (code != 54) // RShift has > 256 code, but state is 0/1
                    st += 2;
            }

            stroke.key.code = code;
            stroke.key.state = (ushort)st;
            ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);
        }
        #endregion

        // ScanCode notes: https://www.win.tue.nl/~aeb/linux/kbd/scancodes-1.html
        public override void ProcessStroke(List<ManagedWrapper.Stroke> strokes)
        {
            var hasSubscription = false;
            var hasContext = ContextCallback != null;

            // Process any waiting input for this keyboard
            var block = false;

            if (_isFiltered)
            {
                var isKeyMapping = false; // True if this is a mapping to a single key, else it would be a mapping to a whole device
                var processedState = ScanCodeHelper.TranslateScanCodes(strokes);
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
                        //mapping.Callback(code, state);
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
                // If the key was blocked by Subscription Mode, then move on to next key...
                if (block) return;

                // If this key had no subscriptions, but Context Mode is set for this keyboard...
                // ... then set the Context before sending the key
                if (!hasSubscription && hasContext) ContextCallback(1);

                // Pass the key(s) through to the OS.
                for (int i = 0; i < strokes.Count; i++)
                {
                    var stroke = strokes[i];
                    ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);
                }

                // If we are processing Context Mode, then Unset the context variable after sending the key
                if (!hasSubscription && hasContext) ContextCallback(0);
            }
        }
    }
}
