using System;
using System.Collections.Generic;
using System.Threading;
using AutoHotInterception.Helpers;

namespace AutoHotInterception.DeviceHandlers
{
    class MouseHandler : DeviceHandler
    {
        private MappingOptions _mouseMoveAbsoluteMapping;
        private MappingOptions _mouseMoveRelativeMapping;

        private bool _absoluteMode00Reported;

        public MouseHandler(IntPtr deviceContext, int deviceId) : base(deviceContext, deviceId)
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
                && ContextCallback == null
                && _mouseMoveRelativeMapping == null
                && _mouseMoveAbsoluteMapping == null)
            {
                _isFiltered = false;
            }
        }

        #region Subscription Mode
        /// <summary>
        /// Subscribes to Absolute mouse movement
        /// </summary>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        /// <returns></returns>
        public void SubscribeMouseMoveAbsolute(MappingOptions mappingOptions)
        {
            _mouseMoveAbsoluteMapping = mappingOptions;
            if (!mappingOptions.Concurrent && !WorkerThreads.ContainsKey(7))
            {
                WorkerThreads.TryAdd(7, new WorkerThread());
            }
            _isFiltered = true;
        }

        /// <summary>
        /// Unsubscribes from absolute mouse movement
        /// </summary>
        public void UnsubscribeMouseMoveAbsolute()
        {
            if (_mouseMoveAbsoluteMapping == null) return;
            if (!_mouseMoveAbsoluteMapping.Concurrent && WorkerThreads.ContainsKey(7))
            {
                WorkerThreads[7].Dispose();
                WorkerThreads.TryRemove(7, out _);
            }
            _mouseMoveAbsoluteMapping = null;
        }

        /// <summary>
        /// Subscribes to Relative mouse movement
        /// </summary>
        /// <param name="mappingOptions">Options for the subscription (block, callback to fire etc)</param>
        /// <returns></returns>
        public void SubscribeMouseMoveRelative(MappingOptions mappingOptions)
        {
            _mouseMoveRelativeMapping = mappingOptions;
            if (!mappingOptions.Concurrent && !WorkerThreads.ContainsKey(8))
            {
                WorkerThreads.TryAdd(8, new WorkerThread());
            }
            _isFiltered = true;
        }

        /// <summary>
        /// Unsubscribes from relative mouse movement
        /// </summary>
        public void UnsubscribeMouseMoveRelative()
        {
            if (_mouseMoveRelativeMapping == null) return;
            if (!_mouseMoveRelativeMapping.Concurrent && WorkerThreads.ContainsKey(8))
            {
                WorkerThreads[8].Dispose();
                WorkerThreads.TryRemove(8, out _);
            }
            _mouseMoveRelativeMapping = null;
        }
        #endregion

        #region Input Synthesis
        /// <summary>
        /// Sends Mouse button events
        /// </summary>
        /// <param name="btn">Button ID to send</param>
        /// <param name="state">State of the button</param>
        /// <returns></returns>
        public void SendMouseButtonEvent(int btn, int state)
        {
            var stroke = HelperFunctions.MouseButtonAndStateToStroke(btn, state);
            ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);
        }

        /// <summary>
        /// Same as <see cref="SendMouseButtonEvent" />, but sends button events in Absolute mode (with coordinates)
        /// </summary>
        /// <param name="btn">Button ID to send</param>
        /// <param name="state">State of the button</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        public void SendMouseButtonEventAbsolute(int btn, int state, int x, int y)
        {
            var stroke = HelperFunctions.MouseButtonAndStateToStroke(btn, state);
            stroke.mouse.x = x;
            stroke.mouse.y = y;
            stroke.mouse.flags = (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute;
            ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);
        }

        /// <summary>
        /// Sends Absolute Mouse Movement
        /// Note: Creating a new stroke seems to make Absolute input become relative to main monitor
        /// Calling Send on an actual stroke from an Absolute device results in input relative to all monitors
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void SendMouseMoveAbsolute(int x, int y)
        {
            var stroke = new ManagedWrapper.Stroke
            { mouse = { x = x, y = y, flags = (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute } };
            ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);
        }

        /// <summary>
        ///     Sends Relative Mouse Movement
        /// </summary>
        /// <param name="x">X movement</param>
        /// <param name="y">Y movement</param>
        /// <returns></returns>
        public void SendMouseMoveRelative(int x, int y)
        {
            var stroke = new ManagedWrapper.Stroke
            { mouse = { x = x, y = y, flags = (ushort)ManagedWrapper.MouseFlag.MouseMoveRelative } };
            ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);
        }
        #endregion

        public override void ProcessStroke(List<ManagedWrapper.Stroke> strokes)
        {
            for (int i = 0; i < strokes.Count; i++)
            {
                var stroke = strokes[i];
                var hasSubscription = false;
                var hasContext = ContextCallback != null;

                var moveRemoved = false;
                var hasMove = false;

                var x = stroke.mouse.x;
                var y = stroke.mouse.y;

                // Process mouse movement
                var isAbsolute = (stroke.mouse.flags & (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute) ==
                                 (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute;
                //Determine whether or not to report mouse movement.
                // For Relative mode, this is fairly simple - if x and y are both 0, no movement was reported (Since a real mouse never reports x=0/y=0)
                // For Absolute mode, x=0/y=0 is reported, but we should limit this to only reporting once...
                // ... so when x=0/y=0 is seen in absolute mode, set the flag _absoluteMode00Reported to true and allow it to be reported...
                // then on subsequent reports of x=0/y=0 for absolute mode, if _absoluteMode00Reported is already true, then do not report movement...
                // ... In absolute mode, when x!=0/y!=0 is received, clear the _absoluteMode00Reported flag
                if (isAbsolute)
                {
                    if (x == 0 && y == 0)
                    {
                        if (!_absoluteMode00Reported)
                        {
                            hasMove = true;
                            _absoluteMode00Reported = true;
                        }
                        else
                        {
                            hasMove = false;
                        }
                    }
                    else
                    {
                        hasMove = true;
                        _absoluteMode00Reported = false;
                    }
                }
                else
                {
                    hasMove = (x != 0 || y != 0);
                }

                if (hasMove)
                {
                    // Process Absolute Mouse Move
                    if (isAbsolute)
                    {
                        if (_mouseMoveAbsoluteMapping != null)
                        {
                            var mapping = _mouseMoveAbsoluteMapping;
                            hasSubscription = true;
                            //var debugStr = $"AHK| Mouse stroke has absolute move of {x}, {y}...";

                            if (mapping.Concurrent)
                                ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                            else if (WorkerThreads.ContainsKey(7))
                                WorkerThreads[7]?.Actions.Add(() => mapping.Callback(x, y));
                            if (mapping.Block)
                            {
                                moveRemoved = true;
                                stroke.mouse.x = 0;
                                stroke.mouse.y = 0;
                                //debugStr += "Blocking";
                            }
                            else
                            {
                                //debugStr += "Not Blocking";
                            }

                            //Debug.WriteLine(debugStr);
                        }
                    }
                    else
                    {
                        if (_mouseMoveRelativeMapping != null)
                        {
                            var mapping = _mouseMoveRelativeMapping;
                            hasSubscription = true;
                            //var debugStr = $"AHK| Mouse stroke has relative move of {x}, {y}...";

                            if (mapping.Concurrent)
                                ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                            else if (WorkerThreads.ContainsKey(8))
                                WorkerThreads[8]?.Actions.Add(() => mapping.Callback(x, y));
                            if (mapping.Block)
                            {
                                moveRemoved = true;
                                stroke.mouse.x = 0;
                                stroke.mouse.y = 0;
                                //debugStr += "Blocking";
                            }
                            else
                            {
                                //debugStr += "Not Blocking";
                            }

                            //Debug.WriteLine(debugStr);
                        }
                    }

                }

                var isMouseButtonsMapping = AllButtonsMapping != null;

                // Process Mouse Buttons - do this AFTER mouse movement, so that absolute mode has coordinates available at the point that the button callback is fired
                if (stroke.mouse.state != 0 && SingleButtonMappings.Count > 0 || isMouseButtonsMapping)
                {
                    var btnStates = HelperFunctions.MouseStrokeToButtonStates(stroke);
                    foreach (var btnState in btnStates)
                    {
                        if (!isMouseButtonsMapping && !SingleButtonMappings.ContainsKey(btnState.Button))
                            continue;

                        hasSubscription = true;
                        MappingOptions mapping = null;
                        if (isMouseButtonsMapping)
                        {
                            mapping = AllButtonsMapping;
                        }
                        else
                        {
                            mapping = SingleButtonMappings[btnState.Button];
                        }

                        var state = btnState;

                        if (mapping.Concurrent)
                        {
                            if (isMouseButtonsMapping)
                            {
                                ThreadPool.QueueUserWorkItem(threadProc =>
                                    mapping.Callback(btnState.Button, state.State));
                            }
                            else
                            {
                                ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(state.State));
                            }
                        }
                        else
                        {
                            if (isMouseButtonsMapping)
                            {
                                DeviceWorkerThread?.Actions
                                    .Add(() => mapping.Callback(btnState.Button, state.State));
                            }
                            else
                            {
                                WorkerThreads[btnState.Button]?.Actions
                                    .Add(() => mapping.Callback(state.State));
                            }
                        }


                        if (mapping.Block)
                        {
                            // Remove the event for this button from the stroke, leaving other button events intact
                            stroke.mouse.state -= btnState.Flag;
                            // If we are removing a mouse wheel event, then set rolling to 0 if no mouse wheel event left
                            if (btnState.Flag == 0x400 || btnState.Flag == 0x800)
                            {
                                if ((stroke.mouse.state & 0x400) != 0x400 &&
                                    (stroke.mouse.state & 0x800) != 0x800)
                                {
                                    //Debug.WriteLine("AHK| Removing rolling flag from stroke");
                                    stroke.mouse.rolling = 0;
                                }
                            }

                            //Debug.WriteLine($"AHK| Removing flag {btnState.Flag} from stoke, leaving state {stroke.mouse.state}");
                        }
                        else
                        {
                            //Debug.WriteLine($"AHK| Leaving flag {btnState.Flag} in stroke");
                        }
                    }
                }

                // Forward on the stroke if required
                if (hasSubscription)
                {
                    // Subscription mode
                    // If the stroke has a move that was not removed, OR it has remaining button events, then forward on the stroke
                    if ((hasMove && !moveRemoved) || stroke.mouse.state != 0)
                    {
                        //Debug.WriteLine($"AHK| Sending stroke. State = {stroke.mouse.state}. hasMove={hasMove}, moveRemoved={moveRemoved}");
                        ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);
                    }
                    else
                    {
                        // Everything removed from stroke, do not forward
                        //Debug.WriteLine("AHK| Mouse stroke now empty, not forwarding");
                    }
                }
                else if (hasContext)
                {
                    // Context Mode - forward stroke with context wrapping
                    ContextCallback(1);
                    ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);
                    ContextCallback(0);
                }
                else
                {
                    // No subscription or context mode - forward on
                    //Debug.WriteLine($"AHK| Sending stroke. State = {stroke.mouse.state}. hasMove={hasMove}, moveRemoved={moveRemoved}");
                    ManagedWrapper.Send(DeviceContext, DeviceId, ref stroke, 1);
                }
            }

        }
    }
}
