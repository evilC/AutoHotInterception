using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using AutoHotInterception.Helpers;

namespace AutoHotInterception
{
    public class Manager : IDisposable
    {
        private readonly ConcurrentDictionary<int, dynamic>
            _contextCallbacks = new ConcurrentDictionary<int, dynamic>();

        private readonly IntPtr _deviceContext;

        // If a device ID exists as a key in this Dictionary then that device is filtered.
        // Used by IsMonitoredDevice, which is handed to Interception as a "Predicate".
        private readonly ConcurrentDictionary<int, bool> _filteredDevices = new ConcurrentDictionary<int, bool>();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>> _keyboardKeyMappings =
            new ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>>();

        private readonly ConcurrentDictionary<int, MappingOptions> _keyboardMappings = 
            new ConcurrentDictionary<int, MappingOptions>();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>> _mouseButtonMappings =
            new ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>>();

        private readonly ConcurrentDictionary<int, MappingOptions> _mouseButtonsMappings = 
            new ConcurrentDictionary<int, MappingOptions>();

        private readonly ConcurrentDictionary<int, MappingOptions> _mouseMoveAbsoluteMappings =
            new ConcurrentDictionary<int, MappingOptions>();

        private readonly ConcurrentDictionary<int, MappingOptions> _mouseMoveRelativeMappings =
            new ConcurrentDictionary<int, MappingOptions>();

        // If an event is subscribed to with concurrent set to false then use a single worker thread to process each event.
        // Makes sure the events are handled synchronously and with a FIFO order.
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, WorkerThread>> _workerThreads =
            new ConcurrentDictionary<int, ConcurrentDictionary<ushort, WorkerThread>>();
        private readonly ConcurrentDictionary<int, WorkerThread> _deviceWorkerThreads = 
            new ConcurrentDictionary<int, WorkerThread>();

        private readonly MultimediaTimer _timer;
        private readonly int _pollRate = 1;
        private volatile bool _lctr = false;
        private volatile bool _pollThreadRunning;
        private bool _absoluteMode00Reported;

        #region Public

        #region Initialization

        public Manager()
        {
            _deviceContext = ManagedWrapper.CreateContext();
            _timer = new MultimediaTimer() { Interval = _pollRate };
            _timer.Elapsed += DoPoll;
        }

        public void Dispose()
        {
            SetThreadState(false);
        }

        public string OkCheck()
        {
            return "OK";
        }

        public void SetLctrl(bool l)
        {
            _lctr = l;
        }

        public void SetState(bool state)
        {
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

            if (!_keyboardKeyMappings.ContainsKey(id))
                _keyboardKeyMappings.TryAdd(id, new ConcurrentDictionary<ushort, MappingOptions>());

            _keyboardKeyMappings[id].TryAdd(code,
                new MappingOptions {Block = block, Concurrent = concurrent, Callback = callback});

            if (!concurrent)
            {
                if (!_workerThreads.ContainsKey(id))
                    _workerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                _workerThreads[id].TryAdd(code, new WorkerThread());
                _workerThreads[id][code].Start();
            }

            SetDeviceFilterState(id, true);
            SetFilterState(true);
            SetThreadState(true);
        }

        public void UnsubscribeKey(int id, ushort code)
        {
            HelperFunctions.IsValidDeviceId(false, id);
            SetFilterState(false);

            if (_keyboardKeyMappings.TryGetValue(id, out var thisDevice))
            {
                thisDevice.TryRemove(code, out _);
                if (thisDevice.Count == 0)
                {
                    _keyboardKeyMappings.TryRemove(id, out _);
                    // Don't remove filter if all keys subscribed
                    if (!_keyboardMappings.ContainsKey(id))
                    {
                        SetDeviceFilterState(id, false);
                    }
                }
            }

            SetFilterState(true);
            SetThreadState(true);
        }

        public void SubscribeKeyboard(int id, bool block, dynamic callback, bool concurrent = false)
        {
            HelperFunctions.IsValidDeviceId(false, id);
            SetFilterState(false);

            _keyboardMappings.TryAdd(id, new MappingOptions { Block = block, Concurrent = concurrent, Callback = callback });
            if (!concurrent)
            {
                _deviceWorkerThreads.TryAdd(id, new WorkerThread());
                _deviceWorkerThreads[id].Start();
            }
            SetDeviceFilterState(id, true);
            SetFilterState(true);
            SetThreadState(true);
        }

        public void UnsubscribeKeyboard(int id)
        {
            HelperFunctions.IsValidDeviceId(false, id);
            SetFilterState(false);

            _keyboardMappings.TryRemove(id, out _);
            if (!_keyboardKeyMappings.ContainsKey(id))
            {
                SetDeviceFilterState(id, false);
            }

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        ///     Subscribe to a Mouse button
        /// </summary>
        /// <param name="id">The ID of the mouse</param>
        /// <param name="btn">The button number (LMB = 0, RMB = 1, MMB = 2, X1 = 3, X2 = 4, WV = 5, WH = 6)</param>
        /// <param name="block">Whether or not to block the button</param>
        /// <param name="callback">The callback to fire when the button changes state</param>
        /// <param name="concurrent">Whether or not to execute callbacks concurrently</param>
        /// <returns></returns>
        public void SubscribeMouseButton(int id, ushort btn, bool block, dynamic callback, bool concurrent = false)
        {
            HelperFunctions.IsValidDeviceId(true, id);

            if (!_mouseButtonMappings.ContainsKey(id))
                _mouseButtonMappings.TryAdd(id, new ConcurrentDictionary<ushort, MappingOptions>());

            _mouseButtonMappings[id].TryAdd(btn,
                new MappingOptions {Block = block, Concurrent = concurrent, Callback = callback});

            if (!concurrent)
            {
                if (!_workerThreads.ContainsKey(id))
                    _workerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                _workerThreads[id].TryAdd(btn, new WorkerThread());
                _workerThreads[id][btn].Start();
            }

            SetDeviceFilterState(id, true);
            SetFilterState(true);
            SetThreadState(true);
        }

        public void UnsubscribeMouseButton(int id, ushort btn)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            if (_mouseButtonMappings.TryGetValue(id, out var thisDevice))
            {
                thisDevice.TryRemove(btn, out _);
                if (thisDevice.Count == 0)
                {
                    _mouseButtonMappings.TryRemove(id, out _);
                    if (!_mouseButtonsMappings.ContainsKey(id))
                    {
                        // Don't remove filter if all buttons subscribed
                        SetDeviceFilterState(id, false);
                    }
                }
            }

            SetFilterState(true);
            SetThreadState(true);
        }

        public void SubscribeMouseButtons(int id, bool block, dynamic callback, bool concurrent = false)
        {
            HelperFunctions.IsValidDeviceId(true, id);

            _mouseButtonsMappings.TryAdd(id,
                new MappingOptions { Block = block, Concurrent = concurrent, Callback = callback });

            if (!concurrent)
            {
                _deviceWorkerThreads.TryAdd(id, new WorkerThread());
                _deviceWorkerThreads[id].Start();
            }

            SetDeviceFilterState(id, true);
            SetFilterState(true);
            SetThreadState(true);
        }

        public void UnsubscribeMouseButtons(int id)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            if (!_mouseButtonMappings.ContainsKey(id))
            {
                SetDeviceFilterState(id, false);
            }

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

            _mouseMoveAbsoluteMappings[id] = new MappingOptions
                {Block = block, Concurrent = concurrent, Callback = callback};

            if (!concurrent)
            {
                if (!_workerThreads.ContainsKey(id))
                    _workerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                _workerThreads[id].TryAdd(7, new WorkerThread()); // Use 7 as second index for MouseMoveAbsolute
                _workerThreads[id][7].Start();
            }

            SetDeviceFilterState(id, true);
            SetFilterState(true);
            SetThreadState(true);
        }

        public void UnsubscribeMouseMoveAbsolute(int id)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            if (_mouseMoveAbsoluteMappings.TryRemove(id, out _))
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

            _mouseMoveRelativeMappings[id] = new MappingOptions
                {Block = block, Concurrent = concurrent, Callback = callback};

            if (!concurrent)
            {
                if (!_workerThreads.ContainsKey(id))
                    _workerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                _workerThreads[id].TryAdd(8, new WorkerThread()); // Use 8 as second index for MouseMoveRelative
                _workerThreads[id][8].Start();
            }

            SetDeviceFilterState(id, true);
            SetFilterState(true);
            SetThreadState(true);
        }

        public void UnsubscribeMouseMoveRelative(int id)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            if (_mouseMoveRelativeMappings.TryRemove(id, out _))
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

            _contextCallbacks[id] = callback;

            SetDeviceFilterState(id, true);
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
            stroke.key.state = (ushort) st;
            ManagedWrapper.Send(_deviceContext, id, ref stroke, 1);
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
            ManagedWrapper.Send(_deviceContext, id, ref stroke, 1);
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
            stroke.mouse.flags = (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute;
            ManagedWrapper.Send(_deviceContext, id, ref stroke, 1);
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
                {mouse = {x = x, y = y, flags = (ushort) ManagedWrapper.MouseFlag.MouseMoveRelative}};
            ManagedWrapper.Send(_deviceContext, id, ref stroke, 1);
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
                {mouse = {x = x, y = y, flags = (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute}};
            ManagedWrapper.Send(_deviceContext, id, ref stroke, 1);
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
            return HelperFunctions.GetDeviceIdFromHandle(_deviceContext, false, handle, instance);
        }

        public int GetMouseIdFromHandle(string handle, int instance = 1)
        {
            return HelperFunctions.GetDeviceIdFromHandle(_deviceContext, true, handle, instance);
        }

        public int GetDeviceIdFromHandle(bool isMouse, string handle, int instance = 1)
        {
            return HelperFunctions.GetDeviceIdFromHandle(_deviceContext, isMouse, handle, instance);
        }

        public int GetDeviceId(bool isMouse, int vid, int pid, int instance = 1)
        {
            return HelperFunctions.GetDeviceId(_deviceContext, isMouse, vid, pid, instance);
        }

        /// <summary>
        ///     Gets a list of connected devices
        ///     Intended to be used called via the AHK wrapper...
        ///     ... so it can convert the return value into an AHK array
        /// </summary>
        /// <returns></returns>
        public HelperFunctions.DeviceInfo[] GetDeviceList()
        {
            return HelperFunctions.GetDeviceList(_deviceContext);
        }

        #endregion

        #endregion Public

        #region Private

        private void SetThreadState(bool state)
        {
            if (state && !_timer.IsRunning)
            {
                SetFilterState(true);
                _timer.Start();
            }
            else if (!state && _timer.IsRunning)
            {
                SetFilterState(false);
                _timer.Stop();
                while (_pollThreadRunning) // Are we mid-poll?
                {
                    Thread.Sleep(10); // Wait until poll ends
                }
            }
        }

        /// <summary>
        ///     Predicate used by Interception to decide whether to filter this device or not.
        ///     WARNING! Setting this to always return true is RISKY, as you could lock yourself out of Windows...
        ///     ... requiring a reboot.
        ///     When working with AHI, it's generally best to keep this matching as little as possible....
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private int IsMonitoredDevice(int device)
        {
            return Convert.ToInt32(_filteredDevices.ContainsKey(device));
        }

        private void SetFilterState(bool state)
        {
            ManagedWrapper.SetFilter(_deviceContext, IsMonitoredDevice,
                state ? ManagedWrapper.Filter.All : ManagedWrapper.Filter.None);
        }

        private void SetDeviceFilterState(int device, bool state)
        {
            if (state && !_filteredDevices.ContainsKey(device))
                _filteredDevices[device] = true;
            else if (!state && _filteredDevices.ContainsKey(device))
                _filteredDevices.TryRemove(device, out _);
        }

        private bool DeviceHasBindings(int id)
        {
            if (id < 11)
                return _keyboardKeyMappings.ContainsKey(id);

            return _mouseButtonMappings.ContainsKey(id)
                   || _mouseMoveRelativeMappings.ContainsKey(id)
                   || _mouseMoveAbsoluteMappings.ContainsKey(id);
        }

        // ScanCode notes: https://www.win.tue.nl/~aeb/linux/kbd/scancodes-1.html
        private void DoPoll(object sender, EventArgs e)
        {
            _pollThreadRunning = true;
            var stroke = new ManagedWrapper.Stroke();

            // Iterate through all Keyboards
            for (var i = 1; i < 11; i++)
            {
                var isMonitoredKeyboard = IsMonitoredDevice(i) == 1;
                var hasSubscription = false;
                var hasContext = _contextCallbacks.ContainsKey(i);

                // Process any waiting input for this keyboard
                while (ManagedWrapper.Receive(_deviceContext, i, ref stroke, 1) > 0)
                {
                    var block = false;
                    // If this is not a monitored keyboard, skip.
                    // This check should not really be needed as the IsMonitoredDevice() predicate should only match monitored keyboards...
                    // ... but in case it does, we want to ignore this bit and pass the input through
                    if (isMonitoredKeyboard)
                    {
                        var isKeyMapping = false; // True if this is a mapping to a single key, else it would be a mapping to a whole device
                        var processedState = HelperFunctions.KeyboardStrokeToKeyboardState(stroke, _lctr);
                        var code = processedState.Code;
                        var state = processedState.State;
                        MappingOptions mapping = null;

                        if (processedState.ChangeLctrl == 1)
                        {
                            _lctr = true;
                        }
                        else if (processedState.ChangeLctrl == 2)
                        {
                            _lctr = false;
                        }

                        if (_keyboardMappings.ContainsKey(i))
                        {
                            mapping = _keyboardMappings[i];
                        }
                        else if (_keyboardKeyMappings.ContainsKey(i) && _keyboardKeyMappings[i].ContainsKey(code))
                        {
                            isKeyMapping = true;
                            mapping = _keyboardKeyMappings[i][code];
                        }
                        if (mapping != null)
                        {
                            // Process Subscription Mode

                            #region KeyCode, State, Extended Flag translation

                            // Begin translation of incoming key code, state, extended flag etc...
                            var processMappings = true;

                            #endregion

                            if (processedState.Ignore)
                            {
                                // Set flag to stop Context Mode from firing
                                hasSubscription = true;
                                // Set flag to indicate disable mapping processing
                                processMappings = false;
                            }

                            // Code and state now normalized, proceed with checking for subscriptions...
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
                                        _workerThreads[i][code]?.Actions.Add(() => mapping.Callback(state));
                                    }
                                    else
                                    {
                                        _deviceWorkerThreads[i]?.Actions.Add(() => mapping.Callback(code, state));
                                    }
                                }
                            }
                        }
                    }

                    // If the key was blocked by Subscription Mode, then move on to next key...
                    if (block) continue;

                    // If this key had no subscriptions, but Context Mode is set for this keyboard...
                    // ... then set the Context before sending the key
                    if (!hasSubscription && hasContext) _contextCallbacks[i](1);

                    // Pass the key through to the OS.
                    ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);

                    // If we are processing Context Mode, then Unset the context variable after sending the key
                    if (!hasSubscription && hasContext) _contextCallbacks[i](0);
                }
            }

            // Process Mice
            for (var i = 11; i < 21; i++)
            {
                var isMonitoredMouse = IsMonitoredDevice(i) == 1;
                var hasSubscription = false;
                var hasContext = _contextCallbacks.ContainsKey(i);

                while (ManagedWrapper.Receive(_deviceContext, i, ref stroke, 1) > 0)
                {
                    var moveRemoved = false;
                    var hasMove = false;
                    if (isMonitoredMouse)
                    {
                        var x = stroke.mouse.x;
                        var y = stroke.mouse.y;
                        //Debug.WriteLine($"AHK| Stroke Seen. State = {stroke.mouse.state}, Flags = {stroke.mouse.flags}, x={x}, y={y}");

                        // Process mouse movement
                        var isAbsolute = (stroke.mouse.flags & (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute) ==
                                         (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute;
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
                                if (_mouseMoveAbsoluteMappings.ContainsKey(i))
                                {
                                    var mapping = _mouseMoveAbsoluteMappings[i];
                                    hasSubscription = true;
                                    //var debugStr = $"AHK| Mouse stroke has absolute move of {x}, {y}...";

                                    if (mapping.Concurrent)
                                        ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                                    else if (_workerThreads.ContainsKey(i) && _workerThreads[i].ContainsKey(7))
                                        _workerThreads[i][7]?.Actions.Add(() => mapping.Callback(x, y));
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
                                if (_mouseMoveRelativeMappings.ContainsKey(i))
                                {
                                    var mapping = _mouseMoveRelativeMappings[i];
                                    hasSubscription = true;
                                    //var debugStr = $"AHK| Mouse stroke has relative move of {x}, {y}...";

                                    if (mapping.Concurrent)
                                        ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                                    else if (_workerThreads.ContainsKey(i) && _workerThreads[i].ContainsKey(8))
                                        _workerThreads[i][8]?.Actions.Add(() => mapping.Callback(x, y));
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


                        var isMouseButtonsMapping = _mouseButtonsMappings.ContainsKey(i);

                        // Process Mouse Buttons - do this AFTER mouse movement, so that absolute mode has coordinates available at the point that the button callback is fired
                        if (stroke.mouse.state != 0 && _mouseButtonMappings.ContainsKey(i) || isMouseButtonsMapping)
                        {
                            var btnStates = HelperFunctions.MouseStrokeToButtonStates(stroke);
                            foreach (var btnState in btnStates)
                            {
                                if (!isMouseButtonsMapping && !_mouseButtonMappings[i].ContainsKey(btnState.Button))
                                    continue;

                                hasSubscription = true;
                                MappingOptions mapping = null;
                                if (isMouseButtonsMapping)
                                {
                                    mapping = _mouseButtonsMappings[i];
                                }
                                else
                                {
                                    mapping = _mouseButtonMappings[i][btnState.Button];
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
                                        _deviceWorkerThreads[i]?.Actions
                                            .Add(() => mapping.Callback(btnState.Button, state.State));
                                    }
                                    else
                                    {
                                        _workerThreads[i][btnState.Button]?.Actions
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
                    }

                    // Forward on the stroke if required
                    if (hasSubscription)
                    {
                        // Subscription mode
                        // If the stroke has a move that was not removed, OR it has remaining button events, then forward on the stroke
                        if ((hasMove && !moveRemoved) || stroke.mouse.state != 0)
                        {
                            //Debug.WriteLine($"AHK| Sending stroke. State = {stroke.mouse.state}. hasMove={hasMove}, moveRemoved={moveRemoved}");
                            ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
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
                        _contextCallbacks[i](1);
                        ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
                        _contextCallbacks[i](0);
                    }
                    else
                    {
                        // No subscription or context mode - forward on
                        //Debug.WriteLine($"AHK| Sending stroke. State = {stroke.mouse.state}. hasMove={hasMove}, moveRemoved={moveRemoved}");
                        ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
                    }
                    //Debug.WriteLine($"AHK| ");
                }
            }

            _pollThreadRunning = false;
        }

        private class MappingOptions
        {
            public bool Block { get; set; }
            public bool Concurrent { get; set; }
            public dynamic Callback { get; set; }
        }

        private class WorkerThread : IDisposable
        {
            private readonly Thread _worker;
            private volatile bool _running;

            public WorkerThread()
            {
                Actions = new BlockingCollection<Action>();
                _worker = new Thread(Run);
                _running = false;
            }

            public BlockingCollection<Action> Actions { get; }

            public void Dispose()
            {
                if (!_running) return;
                _running = false;
                _worker.Join();
            }

            public void Start()
            {
                if (_running) return;
                _running = true;
                _worker.Start();
            }

            private void Run()
            {
                while (_running)
                {
                    var action = Actions.Take();
                    action.Invoke();
                }
            }
        }

        #endregion
    }
}
