using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using AutoHotInterception.DeviceHandlers;
using AutoHotInterception.Helpers;

namespace AutoHotInterception
{
    public class Manager : IDisposable
    {
        private static readonly ConcurrentDictionary<int, dynamic>
            ContextCallbacks = new ConcurrentDictionary<int, dynamic>();

        private static readonly IntPtr DeviceContext = ManagedWrapper.CreateContext();

        // If a device ID exists as a key in this Dictionary then that device is filtered.
        // Used by IsMonitoredDevice, which is handed to Interception as a "Predicate".
        //private static readonly ConcurrentDictionary<int, bool> FilteredDevices = new ConcurrentDictionary<int, bool>();

        private static readonly ConcurrentDictionary<int, IDeviceHandler> DeviceHandlers = new ConcurrentDictionary<int, IDeviceHandler>();

        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>> KeyboardKeyMappings =
            new ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>>();

        private static readonly ConcurrentDictionary<int, MappingOptions> KeyboardMappings =
            new ConcurrentDictionary<int, MappingOptions>();

        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>> MouseButtonMappings =
            new ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>>();

        private static readonly ConcurrentDictionary<int, MappingOptions> MouseButtonsMappings =
            new ConcurrentDictionary<int, MappingOptions>();

        private static readonly ConcurrentDictionary<int, MappingOptions> MouseMoveAbsoluteMappings =
            new ConcurrentDictionary<int, MappingOptions>();

        private static readonly ConcurrentDictionary<int, MappingOptions> MouseMoveRelativeMappings =
            new ConcurrentDictionary<int, MappingOptions>();

        // If an event is subscribed to with concurrent set to false then use a single worker thread to process each event.
        // Makes sure the events are handled synchronously and with a FIFO order.
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, WorkerThread>> WorkerThreads =
            new ConcurrentDictionary<int, ConcurrentDictionary<ushort, WorkerThread>>();
        private static readonly ConcurrentDictionary<int, WorkerThread> DeviceWorkerThreads =
            new ConcurrentDictionary<int, WorkerThread>();

        private static bool _absoluteMode00Reported;

        private static bool _pollThreadRunning;
        private CancellationTokenSource _cancellationToken;

        #region Public

        #region Initialization

        public Manager()
        {
            for (int i = 1; i < 11; i++)
            {
                DeviceHandlers.TryAdd(i, new KeyboardHandler(DeviceContext, i));
            }
            for (int i = 11; i < 21; i++)
            {
                DeviceHandlers.TryAdd(i, new MouseHandler(DeviceContext, i));
            }
        }

        public void Dispose()
        {
            SetState(false);
        }

        /// <summary>
        /// Used by AHK code to make sure it can communicate with AHI
        /// </summary>
        /// <returns></returns>
        public string OkCheck()
        {
            return "OK";
        }

        public void SetState(bool state)
        {
            // Turn off the filter before turning off the thread...
            // .. this is to give the PollThread a chance to finish processing any buffered input
            SetFilterState(state);
            SetThreadState(state);
        }
        #endregion

        #region Subscription Mode

        /// <summary>
        ///     Subscribes to a Keyboard key
        /// </summary>
        /// <param name="id">The ID of the Keyboard</param>
        /// <param name="code">The ScanCode of the key</param>
        /// <param name="block">Whether or not to block the key</param>
        /// <param name="callback">The callback to fire when the key changes state</param>
        /// <param name="concurrent">Whether or not to execute callbacks concurrently</param>
        /// <returns></returns>
        public void SubscribeKey(int id, ushort code, bool block, dynamic callback, bool concurrent = false)
        {
            HelperFunctions.IsValidDeviceId(false, id);
            SetFilterState(false);

            var handler = DeviceHandlers[id];
            handler.SubscribeSingleButton(code, new MappingOptions { Block = block, Concurrent = concurrent, Callback = callback });

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Unsubscribe from a keyboard key
        /// </summary>
        /// <param name="id">The id of the keyboard</param>
        /// <param name="code">The Scancode of the key</param>
        public void UnsubscribeKey(int id, ushort code)
        {
            HelperFunctions.IsValidDeviceId(false, id);
            SetFilterState(false);

            var handler = DeviceHandlers[id];
            handler.UnsubscribeSingleButton(code);

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Subscribe to all keys on a keyboard
        /// </summary>
        /// <param name="id">The id of the keyboard</param>
        /// <param name="block">Whether or not to block the key</param>
        /// <param name="callback">The callback to fire when the key changes state</param>
        /// <param name="concurrent">Whether or not to execute callbacks concurrently</param>
        public void SubscribeKeyboard(int id, bool block, dynamic callback, bool concurrent = false)
        {
            HelperFunctions.IsValidDeviceId(false, id);
            SetFilterState(false);

            var handler = DeviceHandlers[id];
            handler.SubscribeAllButtons(new MappingOptions { Block = block, Concurrent = concurrent, Callback = callback });

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Remove a SubscribeKeyboard subscription
        /// </summary>
        /// <param name="id">The id of the keyboard</param>
        public void UnsubscribeKeyboard(int id)
        {
            HelperFunctions.IsValidDeviceId(false, id);
            SetFilterState(false);

            var handler = DeviceHandlers[id];
            handler.UnsubscribeAllButtons();

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Subscribe to a specific button on a mouse
        /// </summary>
        /// <param name="id">The ID of the mouse</param>
        /// <param name="code">The button number (LMB = 0, RMB = 1, MMB = 2, X1 = 3, X2 = 4, WV = 5, WH = 6)</param>
        /// <param name="block">Whether or not to block the button</param>
        /// <param name="callback">The callback to fire when the button changes state</param>
        /// <param name="concurrent">Whether or not to execute callbacks concurrently</param>
        /// <returns></returns>
        public void SubscribeMouseButton(int id, ushort code, bool block, dynamic callback, bool concurrent = false)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            var handler = DeviceHandlers[id];
            handler.SubscribeSingleButton(code, new MappingOptions { Block = block, Concurrent = concurrent, Callback = callback });

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Unsubscribes from a specific button on a mouse
        /// </summary>
        /// <param name="id">The ID of the mouse</param>
        /// <param name="code">The button number (LMB = 0, RMB = 1, MMB = 2, X1 = 3, X2 = 4, WV = 5, WH = 6)</param>
        public void UnsubscribeMouseButton(int id, ushort code)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            var handler = DeviceHandlers[id];
            handler.UnsubscribeSingleButton(code);

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Create am AllButtons subscription for the specified mouse
        /// </summary>
        /// <param name="id">The ID of the mouse</param>
        /// <param name="block">Whether or not to block the button</param>
        /// <param name="callback">The callback to fire when the button changes state</param>
        /// <param name="concurrent">Whether or not to execute callbacks concurrently</param>
        public void SubscribeMouseButtons(int id, bool block, dynamic callback, bool concurrent = false)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            var handler = DeviceHandlers[id];
            handler.SubscribeAllButtons(new MappingOptions { Block = block, Concurrent = concurrent, Callback = callback });
            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Remove an AllButtons subscription for the specified mouse
        /// </summary>
        /// <param name="id">The ID of the mouse</param>
        public void UnsubscribeMouseButtons(int id)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            var handler = DeviceHandlers[id];
            handler.UnsubscribeAllButtons();

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        ///     Subscribes to Absolute mouse movement
        /// </summary>
        /// <param name="id">The id of the Mouse</param>
        /// <param name="block">Whether or not to block the movement</param>
        /// <param name="callback">The callback to fire when the mouse moves</param>
        /// <param name="concurrent">Whether or not to execute callbacks concurrently</param>
        /// <returns></returns>
        public void SubscribeMouseMoveAbsolute(int id, bool block, dynamic callback, bool concurrent = false)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            MouseMoveAbsoluteMappings[id] = new MappingOptions
            { Block = block, Concurrent = concurrent, Callback = callback };

            if (!concurrent)
            {
                if (!WorkerThreads.ContainsKey(id))
                    WorkerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                WorkerThreads[id].TryAdd(7, new WorkerThread()); // Use 7 as second index for MouseMoveAbsolute
                WorkerThreads[id][7].Start();
            }

            SetDeviceFilterState(id, true);
            SetFilterState(true);
            SetThreadState(true);
        }

        public void UnsubscribeMouseMoveAbsolute(int id)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            if (MouseMoveAbsoluteMappings.TryRemove(id, out _))
                if (!DeviceHasBindings(id))
                    SetDeviceFilterState(id, false);
            SetFilterState(true);
            SetThreadState(true);
        }

        //Shorthand for SubscribeMouseMoveRelative
        public void SubscribeMouseMove(int id, bool block, dynamic callback, bool concurrent = false)
        {
            SubscribeMouseMoveRelative(id, block, callback, concurrent);
        }

        public void UnsubscribeMouseMove(int id)
        {
            UnsubscribeMouseMoveRelative(id);
        }

        /// <summary>
        ///     Subscribes to Relative mouse movement
        /// </summary>
        /// <param name="id">The id of the Mouse</param>
        /// <param name="block">Whether or not to block the movement</param>
        /// <param name="callback">The callback to fire when the mouse moves</param>
        /// <param name="concurrent">Whether or not to execute callbacks concurrently</param>
        /// <returns></returns>
        public void SubscribeMouseMoveRelative(int id, bool block, dynamic callback, bool concurrent = false)
        {
            HelperFunctions.IsValidDeviceId(true, id);

            MouseMoveRelativeMappings[id] = new MappingOptions
            { Block = block, Concurrent = concurrent, Callback = callback };

            if (!concurrent)
            {
                if (!WorkerThreads.ContainsKey(id))
                    WorkerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                WorkerThreads[id].TryAdd(8, new WorkerThread()); // Use 8 as second index for MouseMoveRelative
                WorkerThreads[id][8].Start();
            }

            SetDeviceFilterState(id, true);
            SetFilterState(true);
            SetThreadState(true);
        }

        public void UnsubscribeMouseMoveRelative(int id)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            if (MouseMoveRelativeMappings.TryRemove(id, out _))
                if (!DeviceHasBindings(id))
                    SetDeviceFilterState(id, false);

            SetFilterState(true);
            SetThreadState(true);
        }

        #endregion

        #region Context Mode

        /// <summary>
        ///     Sets a callback for Context Mode for a given device
        /// </summary>
        /// <param name="id">The ID of the device</param>
        /// <param name="callback">The callback to fire before and after each key or button press</param>
        /// <returns></returns>
        public void SetContextCallback(int id, dynamic callback)
        {
            SetFilterState(false);
            if (id < 1 || id > 20)
                throw new ArgumentOutOfRangeException(nameof(id), "DeviceIds must be between 1 and 20");

            var device = DeviceHandlers[id];
            device.SetContextCallback(callback);

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Removes Context Mode for a given device
        /// </summary>
        public void RemoveContextCallback(int id)
        {
            SetFilterState(false);
            if (id < 1 || id > 20)
                throw new ArgumentOutOfRangeException(nameof(id), "DeviceIds must be between 1 and 20");

            if (id < 11)
            {
                var device = (KeyboardHandler)DeviceHandlers[id];
                device.RemoveContextCallback();
            }
            else
            {

            }

            SetFilterState(true);
            SetThreadState(true);
        }

        #endregion

        #region Input Synthesis

        /// <summary>
        ///     Sends a keyboard key event
        /// </summary>
        /// <param name="id">The ID of the Keyboard to send as</param>
        /// <param name="code">The ScanCode to send</param>
        /// <param name="state">The State to send (1 = pressed, 0 = released)</param>
        public void SendKeyEvent(int id, ushort code, int state)
        {
            HelperFunctions.IsValidDeviceId(false, id);
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
            ManagedWrapper.Send(DeviceContext, id, ref stroke, 1);
        }

        /// <summary>
        ///     Sends Mouse button events
        /// </summary>
        /// <param name="id"></param>
        /// <param name="btn"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public void SendMouseButtonEvent(int id, int btn, int state)
        {
            HelperFunctions.IsValidDeviceId(true, id);

            var stroke = HelperFunctions.MouseButtonAndStateToStroke(btn, state);
            ManagedWrapper.Send(DeviceContext, id, ref stroke, 1);
        }

        /// <summary>
        ///     Same as <see cref="SendMouseButtonEvent" />, but sends button events in Absolute mode (with coordinates)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="btn"></param>
        /// <param name="state"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SendMouseButtonEventAbsolute(int id, int btn, int state, int x, int y)
        {
            var stroke = HelperFunctions.MouseButtonAndStateToStroke(btn, state);
            stroke.mouse.x = x;
            stroke.mouse.y = y;
            stroke.mouse.flags = (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute;
            ManagedWrapper.Send(DeviceContext, id, ref stroke, 1);
        }

        public void SendMouseMove(int id, int x, int y)
        {
            SendMouseMoveRelative(id, x, y);
        }

        /// <summary>
        ///     Sends Relative Mouse Movement
        /// </summary>
        /// <param name="id"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void SendMouseMoveRelative(int id, int x, int y)
        {
            HelperFunctions.IsValidDeviceId(true, id);

            var stroke = new ManagedWrapper.Stroke
            { mouse = { x = x, y = y, flags = (ushort)ManagedWrapper.MouseFlag.MouseMoveRelative } };
            ManagedWrapper.Send(DeviceContext, id, ref stroke, 1);
        }

        /// <summary>
        ///     Sends Absolute Mouse Movement
        ///     Note: Creating a new stroke seems to make Absolute input become relative to main monitor
        ///     Calling Send on an actual stroke from an Absolute device results in input relative to all monitors
        /// </summary>
        /// <param name="id"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void SendMouseMoveAbsolute(int id, int x, int y)
        {
            HelperFunctions.IsValidDeviceId(true, id);

            var stroke = new ManagedWrapper.Stroke
            { mouse = { x = x, y = y, flags = (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute } };
            ManagedWrapper.Send(DeviceContext, id, ref stroke, 1);
        }

        #endregion

        #region Device Querying

        public int GetKeyboardId(int vid, int pid, int instance = 1)
        {
            return GetDeviceId(false, vid, pid, instance);
        }

        public int GetMouseId(int vid, int pid, int instance = 1)
        {
            return GetDeviceId(true, vid, pid, instance);
        }

        public int GetKeyboardIdFromHandle(string handle, int instance = 1)
        {
            return HelperFunctions.GetDeviceIdFromHandle(DeviceContext, false, handle, instance);
        }

        public int GetMouseIdFromHandle(string handle, int instance = 1)
        {
            return HelperFunctions.GetDeviceIdFromHandle(DeviceContext, true, handle, instance);
        }

        public int GetDeviceIdFromHandle(bool isMouse, string handle, int instance = 1)
        {
            return HelperFunctions.GetDeviceIdFromHandle(DeviceContext, isMouse, handle, instance);
        }

        public int GetDeviceId(bool isMouse, int vid, int pid, int instance = 1)
        {
            return HelperFunctions.GetDeviceId(DeviceContext, isMouse, vid, pid, instance);
        }

        /// <summary>
        ///     Gets a list of connected devices
        ///     Intended to be used called via the AHK wrapper...
        ///     ... so it can convert the return value into an AHK array
        /// </summary>
        /// <returns></returns>
        public HelperFunctions.DeviceInfo[] GetDeviceList()
        {
            return HelperFunctions.GetDeviceList(DeviceContext);
        }

        #endregion

        #endregion Public

        #region Private

        private void SetThreadState(bool state)
        {
            if (state && !_pollThreadRunning)
            {
                _cancellationToken = new CancellationTokenSource();
                ThreadPool.QueueUserWorkItem(PollThread, _cancellationToken.Token);
                while (!_pollThreadRunning)
                {
                    // Wait for PollThread to actually start
                    Thread.Sleep(10);
                }
            }
            else if (!state && _pollThreadRunning)
            {
                _cancellationToken.Cancel();
                _cancellationToken.Dispose();
                while (_pollThreadRunning)
                {
                    // Wait for PollThread to actually stop
                    Thread.Sleep(10);
                }
            }
        }

        /// <summary>
        ///     Predicate used by Interception to decide whether to filter this device or not.
        ///     WARNING! Setting this to always return true is RISKY, as you could lock yourself out of Windows...
        ///     ... requiring a reboot.
        ///     When working with AHI, it's generally best to keep this matching as little as possible....
        /// </summary>
        /// <param name="device">The Interception ID of the device</param>
        /// <returns></returns>
        private static int IsMonitoredDevice(int device)
        {
            //return Convert.ToInt32(FilteredDevices.ContainsKey(device));
            return DeviceHandlers[device].IsFiltered();
        }

        private void SetFilterState(bool state)
        {
            ManagedWrapper.SetFilter(DeviceContext, IsMonitoredDevice,
                state ? ManagedWrapper.Filter.All : ManagedWrapper.Filter.None);
        }

        private void SetDeviceFilterState(int device, bool state)
        {
            //if (state && !FilteredDevices.ContainsKey(device))
            //    FilteredDevices[device] = true;
            //else if (!state && FilteredDevices.ContainsKey(device))
            //    FilteredDevices.TryRemove(device, out _);
            DeviceHandlers[device].SetFilterState(state);
        }

        private bool DeviceHasBindings(int id)
        {
            if (id < 11)
                return KeyboardKeyMappings.ContainsKey(id);

            return MouseButtonMappings.ContainsKey(id)
                   || MouseMoveRelativeMappings.ContainsKey(id)
                   || MouseMoveAbsoluteMappings.ContainsKey(id);
        }

        // ScanCode notes: https://www.win.tue.nl/~aeb/linux/kbd/scancodes-1.html

        private static void PollThread(object obj)
        {
            var token = (CancellationToken)obj;
            //Debug.WriteLine($"AHK| Poll Thread Started");
            _pollThreadRunning = true;
            var stroke = new ManagedWrapper.Stroke();
            int i;
            while (!token.IsCancellationRequested)
            {
                // While no input happens, this loop will exit every 1ms to allow us to check if cancellation has been requested
                // WaitWithTimeout is used with a timeout of 10ms instead of Wait, so that when we eg use SetState to turn the thread off...
                // ... any input which was filtered and is waiting to be processed can be processed (eg lots of mouse moves buffered)
                while (ManagedWrapper.Receive(DeviceContext, i = ManagedWrapper.WaitWithTimeout(DeviceContext, 10), ref stroke, 1) > 0)
                {
                    DeviceHandlers[i].ProcessStroke(stroke);
                    
                    if (false)
                    { 
                        if (i < 11)
                        {

                        }
                        else
                        {
                            // Mice
                            var hasSubscription = false;
                            var hasContext = ContextCallbacks.ContainsKey(i);

                            var moveRemoved = false;
                            var hasMove = false;

                            var x = stroke.mouse.x;
                            var y = stroke.mouse.y;
                            //Debug.WriteLine($"AHK| Stroke Seen. State = {stroke.mouse.state}, Flags = {stroke.mouse.flags}, x={x}, y={y}");

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
                                    if (MouseMoveAbsoluteMappings.ContainsKey(i))
                                    {
                                        var mapping = MouseMoveAbsoluteMappings[i];
                                        hasSubscription = true;
                                        //var debugStr = $"AHK| Mouse stroke has absolute move of {x}, {y}...";

                                        if (mapping.Concurrent)
                                            ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                                        else if (WorkerThreads.ContainsKey(i) && WorkerThreads[i].ContainsKey(7))
                                            WorkerThreads[i][7]?.Actions.Add(() => mapping.Callback(x, y));
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

                                // Process Relative Mouse Move
                                //else if ((stroke.mouse.flags & (ushort) ManagedWrapper.MouseFlag.MouseMoveRelative) == (ushort) ManagedWrapper.MouseFlag.MouseMoveRelative) / flag is 0, so always true!
                                else
                                {
                                    if (MouseMoveRelativeMappings.ContainsKey(i))
                                    {
                                        var mapping = MouseMoveRelativeMappings[i];
                                        hasSubscription = true;
                                        //var debugStr = $"AHK| Mouse stroke has relative move of {x}, {y}...";

                                        if (mapping.Concurrent)
                                            ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                                        else if (WorkerThreads.ContainsKey(i) && WorkerThreads[i].ContainsKey(8))
                                            WorkerThreads[i][8]?.Actions.Add(() => mapping.Callback(x, y));
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


                            var isMouseButtonsMapping = MouseButtonsMappings.ContainsKey(i);

                            // Process Mouse Buttons - do this AFTER mouse movement, so that absolute mode has coordinates available at the point that the button callback is fired
                            if (stroke.mouse.state != 0 && MouseButtonMappings.ContainsKey(i) || isMouseButtonsMapping)
                            {
                                var btnStates = HelperFunctions.MouseStrokeToButtonStates(stroke);
                                foreach (var btnState in btnStates)
                                {
                                    if (!isMouseButtonsMapping && !MouseButtonMappings[i].ContainsKey(btnState.Button))
                                        continue;

                                    hasSubscription = true;
                                    MappingOptions mapping = null;
                                    if (isMouseButtonsMapping)
                                    {
                                        mapping = MouseButtonsMappings[i];
                                    }
                                    else
                                    {
                                        mapping = MouseButtonMappings[i][btnState.Button];
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
                                            DeviceWorkerThreads[i]?.Actions
                                                .Add(() => mapping.Callback(btnState.Button, state.State));
                                        }
                                        else
                                        {
                                            WorkerThreads[i][btnState.Button]?.Actions
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
                                    ManagedWrapper.Send(DeviceContext, i, ref stroke, 1);
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
                                ContextCallbacks[i](1);
                                ManagedWrapper.Send(DeviceContext, i, ref stroke, 1);
                                ContextCallbacks[i](0);
                            }
                            else
                            {
                                // No subscription or context mode - forward on
                                //Debug.WriteLine($"AHK| Sending stroke. State = {stroke.mouse.state}. hasMove={hasMove}, moveRemoved={moveRemoved}");
                                ManagedWrapper.Send(DeviceContext, i, ref stroke, 1);
                            }
                            //Debug.WriteLine($"AHK| ");
                        }
                    }
                }
            }
            _pollThreadRunning = false;
            //Debug.WriteLine($"AHK| Poll Thread Ended");
        }
        #endregion
    }
}