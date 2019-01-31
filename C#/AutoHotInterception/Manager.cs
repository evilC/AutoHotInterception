using System;
using System.Collections.Concurrent;
using System.Threading;
using AutoHotInterception.Helpers;
using static AutoHotInterception.Helpers.HelperFunctions;

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

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>> _keyboardMappings =
            new ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>>();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>> _mouseButtonMappings =
            new ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>>();

        private readonly ConcurrentDictionary<int, MappingOptions> _mouseMoveAbsoluteMappings =
            new ConcurrentDictionary<int, MappingOptions>();

        private readonly ConcurrentDictionary<int, MappingOptions> _mouseMoveRelativeMappings =
            new ConcurrentDictionary<int, MappingOptions>();

        // If an event is subscribed to with concurrent set to false then use a single worker thread to process each event.
        // Makes sure the events are handled synchronously and with a FIFO order.
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, WorkerThread>> _workerThreads =
            new ConcurrentDictionary<int, ConcurrentDictionary<ushort, WorkerThread>>();

        private Thread _pollThread;
        private volatile bool _pollThreadRunning;

        #region Public

        #region Initialization

        public Manager()
        {
            _deviceContext = ManagedWrapper.CreateContext();
        }

        public void Dispose()
        {
            SetThreadState(false);
        }

        public string OkCheck()
        {
            return "OK";
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
            SetFilterState(false);
            IsValidDeviceId(false, id);

            if (!_keyboardMappings.ContainsKey(id))
                _keyboardMappings.TryAdd(id, new ConcurrentDictionary<ushort, MappingOptions>());

            _keyboardMappings[id].TryAdd(code,
                new MappingOptions {Block = block, Concurrent = concurrent, Callback = callback});
            _filteredDevices[id] = true;

            if (!concurrent)
            {
                if (!_workerThreads.ContainsKey(id))
                    _workerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                _workerThreads[id].TryAdd(code, new WorkerThread());
                _workerThreads[id][code].Start();
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
            IsValidDeviceId(true, id);

            if (!_mouseButtonMappings.ContainsKey(id))
                _mouseButtonMappings.TryAdd(id, new ConcurrentDictionary<ushort, MappingOptions>());
            _mouseButtonMappings[id].TryAdd(btn,
                new MappingOptions {Block = block, Concurrent = concurrent, Callback = callback});
            _filteredDevices[id] = true;

            if (!concurrent)
            {
                if (!_workerThreads.ContainsKey(id))
                    _workerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                _workerThreads[id].TryAdd(btn, new WorkerThread());
                _workerThreads[id][btn].Start();
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
            IsValidDeviceId(true, id);

            _mouseMoveAbsoluteMappings[id] = new MappingOptions
                {Block = block, Concurrent = concurrent, Callback = callback};
            _filteredDevices[id] = true;

            if (!concurrent)
            {
                if (!_workerThreads.ContainsKey(id))
                    _workerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                _workerThreads[id].TryAdd(7, new WorkerThread()); // Use 7 as second index for MouseMoveAbsolute
                _workerThreads[id][7].Start();
            }

            SetFilterState(true);
            SetThreadState(true);
        }

        //Shorthand for SubscribeMouseMoveRelative
        public void SubscribeMouseMove(int id, bool block, dynamic callback, bool concurrent = false)
        {
            SubscribeMouseMoveRelative(id, block, callback, concurrent);
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
            IsValidDeviceId(true, id);

            _mouseMoveRelativeMappings[id] = new MappingOptions
                {Block = block, Concurrent = concurrent, Callback = callback};
            _filteredDevices[id] = true;

            if (!concurrent)
            {
                if (!_workerThreads.ContainsKey(id))
                    _workerThreads.TryAdd(id, new ConcurrentDictionary<ushort, WorkerThread>());

                _workerThreads[id].TryAdd(8, new WorkerThread()); // Use 8 as second index for MouseMoveRelative
                _workerThreads[id][8].Start();
            }

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
            _filteredDevices[id] = true;

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
            IsValidDeviceId(false, id);
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
            IsValidDeviceId(true, id);

            var stroke = ButtonAndStateToStroke(btn, state);
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
            var stroke = ButtonAndStateToStroke(btn, state);
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
            IsValidDeviceId(true, id);

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
            IsValidDeviceId(true, id);

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
            return GetDeviceIdFromHandle(false, handle, instance);
        }

        public int GetMouseIdFromHandle(string handle, int instance = 1)
        {
            return GetDeviceIdFromHandle(true, handle, instance);
        }

        /// <summary>
        ///     Tries to get Device ID from VID/PID
        /// </summary>
        /// <param name="isMouse">Whether the device is a mouse or a keyboard</param>
        /// <param name="vid">The VID of the device</param>
        /// <param name="pid">The PID of the device</param>
        /// <param name="instance">The instance of the VID/PID (Optional)</param>
        /// <returns></returns>
        public int GetDeviceId(bool isMouse, int vid, int pid, int instance = 1)
        {
            var start = isMouse ? 11 : 0;
            var max = isMouse ? 21 : 11;
            for (var i = start; i < max; i++)
            {
                var hardwareStr = ManagedWrapper.GetHardwareStr(_deviceContext, i, 1000);
                int foundVid = 0, foundPid = 0;
                GetVidPid(hardwareStr, ref foundVid, ref foundPid);
                if (foundVid != vid || foundPid != pid) continue;
                if (instance == 1) return i;
                instance--;
            }

            //ToDo: Should throw here?
            return 0;
        }

        /// <summary>
        ///     Tries to get Device ID from Hardware String
        /// </summary>
        /// <param name="isMouse">Whether the device is a mouse or a keyboard</param>
        /// <param name="handle">The Hardware String (handle) of the device</param>
        /// <param name="instance">The instance of the VID/PID (Optional)</param>
        /// <returns></returns>
        public int GetDeviceIdFromHandle(bool isMouse, string handle, int instance = 1)
        {
            var start = isMouse ? 11 : 0;
            var max = isMouse ? 21 : 11;
            for (var i = start; i < max; i++)
            {
                var hardwareStr = ManagedWrapper.GetHardwareStr(_deviceContext, i, 1000);
                if (hardwareStr != handle) continue;

                if (instance == 1) return i;
                instance--;
            }

            //ToDo: Should throw here?
            return 0;
        }

        /// <summary>
        ///     Gets a list of connected devices
        ///     Intended to be used called via the AHK wrapper...
        ///     ... so it can convert the return value into an AHK array
        /// </summary>
        /// <returns></returns>
        public DeviceInfo[] GetDeviceList()
        {
            return HelperFunctions.GetDeviceList(_deviceContext);
        }

        #endregion

        #endregion Public

        #region Private

        private void SetThreadState(bool state)
        {
            if (state)
            {
                if (_pollThreadRunning) return;
                _pollThreadRunning = true;
                _pollThread = new Thread(PollThread);
                _pollThread.Start();
            }
            else
            {
                _pollThreadRunning = false;
                _pollThread.Join();
                _pollThread = null;
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

        // ScanCode notes: https://www.win.tue.nl/~aeb/linux/kbd/scancodes-1.html
        private void PollThread()
        {
            var stroke = new ManagedWrapper.Stroke();

            while (_pollThreadRunning)
            {
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
                        if (isMonitoredKeyboard && _keyboardMappings.ContainsKey(i))
                        {
                            // Process Subscription Mode
                            var code = stroke.key.code;
                            var state = stroke.key.state;

                            #region KeyCode, State, Extended Flag translation

                            // Begin translation of incoming key code, state, extended flag etc...
                            var processMappings = true;
                            if (code == 54) code = 310;

                            // If state is shifted up by 2 (1 or 2 instead of 0 or 1), then this is an "Extended" key code
                            if (state > 1)
                            {
                                if (code == 42)
                                {
                                    // Shift (42/0x2a) with extended flag = the key after this one is extended.
                                    // Example case is Delete (The one above the arrow keys, not on numpad)...
                                    // ... this generates a stroke of 0x2a (Shift) with *extended flag set* (Normal shift does not do this)...
                                    // ... followed by 0x53 with extended flag set.
                                    // We do not want to fire subscriptions for the extended shift, but *do* want to let the key flow through...
                                    // ... so that is handled here.
                                    // When the extended key (Delete in the above example) subsequently comes through...
                                    // ... it will have code 0x53, which we shift to 0x153 (Adding 256 Dec) to signify extended version...
                                    // ... as this is how AHK behaves with GetKeySC()

                                    // Set flag to stop Context Mode from firing
                                    hasSubscription = true;
                                    // Set flag to indicate disable mapping processing
                                    processMappings = false;
                                }
                                else
                                {
                                    // Extended flag set
                                    // Shift code up by 256 (0x100) to signify extended code
                                    code += 256;
                                    state -= 2;
                                }
                            }

                            #endregion

                            // Code and state now normalized, proceed with checking for subscriptions...
                            if (processMappings && _keyboardMappings[i].ContainsKey(code))
                            {
                                hasSubscription = true;
                                var mapping = _keyboardMappings[i][code];
                                if (mapping.Block) block = true;
                                if (mapping.Concurrent)
                                    ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(1 - state));
                                else if (_workerThreads.ContainsKey(i) && _workerThreads[i].ContainsKey(code))
                                    _workerThreads[i][code]?.Actions.Add(() => mapping.Callback(1 - state));
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
                        //Debug.WriteLine($"AHK| Mouse {i} seen - flags: {stroke.mouse.flags}, raw state: {stroke.mouse.state}");
                        var block = false;
                        if (isMonitoredMouse)
                        {
                            if (stroke.mouse.state != 0 && _mouseButtonMappings.ContainsKey(i))
                            {
                                // Mouse Button
                                //Debug.WriteLine($"AHK| Mouse {i} seen - flags: {stroke.mouse.flags}, raw state: {stroke.mouse.state}");
                                var btnState = StrokeStateToButtonState(stroke);
                                if (_mouseButtonMappings[i].ContainsKey(btnState.Button))
                                {
                                    hasSubscription = true;
                                    var mapping = _mouseButtonMappings[i][btnState.Button];
                                    if (mapping.Block) block = true;

                                    var state = btnState;

                                    if (mapping.Concurrent)
                                        ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(state.State));
                                    else if (_workerThreads.ContainsKey(i) &&
                                             _workerThreads[i].ContainsKey(btnState.Button))
                                        _workerThreads[i][btnState.Button]?.Actions
                                            .Add(() => mapping.Callback(state.State));
                                }

                                //Console.WriteLine($"AHK| Mouse {i} seen - button {btnState.Button}, state: {stroke.mouse.state}, rolling: {stroke.mouse.rolling}");
                            }
                            else if ((stroke.mouse.flags & (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute) ==
                                     (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute
                                     && _mouseMoveAbsoluteMappings.ContainsKey(i))
                            {
                                // Absolute Mouse Move
                                hasSubscription = true;
                                var mapping = _mouseMoveAbsoluteMappings[i];
                                if (mapping.Block) block = true;

                                var x = stroke.mouse.x;
                                var y = stroke.mouse.y;
                                if (mapping.Concurrent)
                                    ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                                else if (_workerThreads.ContainsKey(i) && _workerThreads[i].ContainsKey(7))
                                    _workerThreads[i][7]?.Actions.Add(() => mapping.Callback(x, y));
                            }
                            else if ((stroke.mouse.flags & (ushort) ManagedWrapper.MouseFlag.MouseMoveRelative) ==
                                     (ushort) ManagedWrapper.MouseFlag.MouseMoveRelative
                                     && _mouseMoveRelativeMappings.ContainsKey(i))
                            {
                                // Relative Mouse Move
                                hasSubscription = true;
                                var mapping = _mouseMoveRelativeMappings[i];
                                if (mapping.Block) block = true;

                                var x = stroke.mouse.x;
                                var y = stroke.mouse.y;
                                if (mapping.Concurrent)
                                    ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                                else if (_workerThreads.ContainsKey(i) && _workerThreads[i].ContainsKey(8))
                                    _workerThreads[i][8]?.Actions.Add(() => mapping.Callback(x, y));
                            }
                        }

                        // If this key had no subscriptions, but Context Mode is set for this mouse...
                        // ... then set the Context before sending the button
                        if (!hasSubscription && hasContext) _contextCallbacks[i](1); // Set Context
                        if (!block) ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
                        // If we are processing Context Mode, then Unset the context variable after sending the button
                        if (!hasSubscription && hasContext) _contextCallbacks[i](0);
                    }
                }

                Thread.Sleep(10);
            }
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