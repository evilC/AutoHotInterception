using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoHotInterception.Helpers;
using static AutoHotInterception.Helpers.HelperFunctions;

namespace AutoHotInterception
{
    public class Manager : IDisposable
    {
        private readonly IntPtr _deviceContext;
        private Thread _pollThread;
        private bool _pollThreadRunning = false;

        private bool _filterState = false;

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>> _keyboardMappings = new ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>>();
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>> _mouseButtonMappings = new ConcurrentDictionary<int, ConcurrentDictionary<ushort, MappingOptions>>();
        private readonly ConcurrentDictionary<int, MappingOptions> _mouseMoveRelativeMappings = new ConcurrentDictionary<int, MappingOptions>();
        private readonly ConcurrentDictionary<int, MappingOptions> _mouseMoveAbsoluteMappings = new ConcurrentDictionary<int, MappingOptions>();
        private readonly ConcurrentDictionary<int, dynamic> _contextCallbacks = new ConcurrentDictionary<int, dynamic>();

        // If a the ID of a device exists as a key in this Dictionary, then that device is filtered.
        // Used by IsMonitoredDevice, which is handed to Interception as a "Predicate".
        private readonly ConcurrentDictionary<int, bool> _filteredDevices = new ConcurrentDictionary<int, bool>();

        #region Public

        #region Initialization

        public Manager()
        {
            _deviceContext = ManagedWrapper.CreateContext();
        }

        public string OkCheck()
        {
            return "OK";
        }

        #endregion

        #region Subscription Mode

        /// <summary>
        /// Subscribes to a Keyboard key
        /// </summary>
        /// <param name="id">The ID of the Keyboard</param>
        /// <param name="code">The ScanCode of the key</param>
        /// <param name="block">Whether or not to block the key</param>
        /// <param name="callback">The callback to fire when the key changes state</param>
        /// <returns></returns>
        public void SubscribeKey(int id, ushort code, bool block, dynamic callback)
        {
            SetFilterState(false);
            IsValidDeviceId(false, id);

            if (!_keyboardMappings.ContainsKey(id))
            {
                _keyboardMappings.TryAdd(id, new ConcurrentDictionary<ushort, MappingOptions>());
            }

            _keyboardMappings[id].TryAdd(code, new MappingOptions() { Block = block, Callback = callback });
            _filteredDevices[id] = true;

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Subscribe to a Mouse button
        /// </summary>
        /// <param name="id">The ID of the mouse</param>
        /// <param name="btn">The button number (LMB = 0, RMB = 1, MMB = 2, X1 = 3, X2 = 4</param>
        /// <param name="block">Whether or not to block the button</param>
        /// <param name="callback">The callback to fire when the button changes state</param>
        /// <returns></returns>
        public void SubscribeMouseButton(int id, ushort btn, bool block, dynamic callback)
        {
            IsValidDeviceId(true, id);

            if (!_mouseButtonMappings.ContainsKey(id))
            {
                _mouseButtonMappings.TryAdd(id, new ConcurrentDictionary<ushort, MappingOptions>());
            }
            _mouseButtonMappings[id].TryAdd(btn, new MappingOptions() { Block = block, Callback = callback });
            _filteredDevices[id] = true;

            SetFilterState(true);
            SetThreadState(true);
        }

        //Shorthand for SubscribeMouseMoveRelative
        public bool SubscribeMouseMove(int id, bool block, dynamic callback)
        {
            return SubscribeMouseMoveRelative(id, block, callback);
        }

        /// <summary>
        /// Subscribes to Relative mouse movement
        /// </summary>
        /// <param name="id">The id of the Mouse</param>
        /// <param name="block">Whether or not to block the movement</param>
        /// <param name="callback">The callback to fire when the mouse moves</param>
        /// <returns></returns>
        public void SubscribeMouseMoveRelative(int id, bool block, dynamic callback)
        {
            IsValidDeviceId(true, id);

            _mouseMoveRelativeMappings[id] = new MappingOptions() { Block = block, Callback = callback };
            _filteredDevices[id] = true;
            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// 
        /// Subscribes to Absolute mouse movement
        /// </summary>
        /// <param name="id">The id of the Mouse</param>
        /// <param name="block">Whether or not to block the movement</param>
        /// <param name="callback">The callback to fire when the mouse moves</param>
        /// <returns></returns>
        public void SubscribeMouseMoveAbsolute(int id, bool block, dynamic callback)
        {
            IsValidDeviceId(true, id);

            _mouseMoveAbsoluteMappings[id] = new MappingOptions() { Block = block, Callback = callback };
            _filteredDevices[id] = true;
            SetFilterState(true);
            SetThreadState(true);
        }

        #endregion

        #region Context Mode

        /// <summary>
        /// Sets a callback for Context Mode for a given device
        /// </summary>
        /// <param name="id">The ID of the device</param>
        /// <param name="callback">The callback to fire before and after each key or button press</param>
        /// <returns></returns>
        public void SetContextCallback(int id, dynamic callback)
        {
            SetFilterState(false);
            if (id < 1 || id > 20)
            {
                throw new ArgumentOutOfRangeException(nameof(id), $"DeviceIds must be between 1 and 20");
            }

            _contextCallbacks[id] = callback;
            _filteredDevices[id] = true;

            SetFilterState(true);
            SetThreadState(true);
        }

        #endregion

        #region Input Synthesis

        /// <summary>
        /// Sends a keyboard key event
        /// </summary>
        /// <param name="id">The ID of the Keyboard to send as</param>
        /// <param name="code">The ScanCode to send</param>
        /// <param name="state">The State to send (1 = pressed, 0 = released)</param>
        public void SendKeyEvent(int id, ushort code, int state)
        {
            IsValidDeviceId(false, id);

            var stroke = new ManagedWrapper.Stroke();
            if (code > 255)
            {
                code -= 256;
                state += 2;
            }
            stroke.key.code = code;
            stroke.key.state = (ushort)(1 - state);
            ManagedWrapper.Send(_deviceContext, id, ref stroke, 1);
        }

        /// <summary>
        /// Sends Mouse button events
        /// </summary>
        /// <param name="id"></param>
        /// <param name="btn"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public void SendMouseButtonEvent(int id, int btn, int state)
        {
            IsValidDeviceId(true, id);

            var stroke = new ManagedWrapper.Stroke {mouse = {state = ButtonAndStateToStrokeState(btn, state)}};
            ManagedWrapper.Send(_deviceContext, id, ref stroke, 1);
        }

        /// <summary>
        /// Same as <see cref="SendMouseButtonEvent"/>, but sends button events in Absolute mode (with coordinates)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="btn"></param>
        /// <param name="state"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SendMouseButtonEventAbsolute(int id, int btn, int state, int x, int y)
        {
            var stroke = new ManagedWrapper.Stroke
            {
                mouse =
                {
                    x = x, y = y, flags = (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute,
                    state = ButtonAndStateToStrokeState(btn, state)
                }
            };
            ManagedWrapper.Send(_deviceContext, id, ref stroke, 1);
        }

        public void SendMouseMove(int id, int x, int y)
        {
            SendMouseMoveRelative(id, x, y);
        }

        /// <summary>
        /// Sends Relative Mouse Movement
        /// </summary>
        /// <param name="id"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void SendMouseMoveRelative(int id, int x, int y)
        {
            IsValidDeviceId(true, id);

            var stroke = new ManagedWrapper.Stroke { mouse = { x = x, y = y, flags = (ushort)ManagedWrapper.MouseFlag.MouseMoveRelative } };
            ManagedWrapper.Send(_deviceContext, id, ref stroke, 1);
        }

        /// <summary>
        /// Sends Absolute  Mouse Movement
        /// Note: Newing up a stroke seems to make Absolute input be relative to main monitor
        /// Calling Send on an actual stroke from an Absolute device results in input relative to all monitors
        /// </summary>
        /// <param name="id"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void SendMouseMoveAbsolute(int id, int x, int y)
        {
            IsValidDeviceId(true, id);

            var stroke = new ManagedWrapper.Stroke { mouse = { x = x, y = y, flags = (ushort)ManagedWrapper.MouseFlag.MouseMoveAbsolute } };
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

        /// <summary>
        /// Tries to get Device ID from VID/PID
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
                var handle = ManagedWrapper.GetHardwareStr(_deviceContext, i, 1000);
                int foundVid = 0, foundPid = 0;
                GetVidPid(handle, ref foundVid, ref foundPid);
                if (foundVid != vid || foundPid != pid) continue;
                if (instance == 1)
                {
                    return i;
                }
                instance--;
            }

            //ToDo: Should throw here?
            return 0;
        }

        /// <summary>
        /// Gets a list of connected devices
        /// Intended to be used called via the AHK wrapper...
        /// ... so it can convert the return value into an AHK array
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
                if (!_pollThreadRunning)
                {
                    _pollThreadRunning = true;
                    _pollThread = new Thread(PollThread);
                    _pollThread.Start();
                }
            }
            else
            {
                _pollThread.Abort();
                _pollThread.Join();
                _pollThread = null;
            }
        }

        /// <summary>
        /// Predicate used by Interception to decide whether to filter this device or not.
        /// WARNING! Setting this to always return true is RISKY, as you could lock yourself out of Windows...
        /// ... requiring a reboot.
        /// When working with AHI, it's generally best to keep this matching as little as possible....
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

            _filterState = state;
        }

        private void PollThread()
        {
            ManagedWrapper.Stroke stroke = new ManagedWrapper.Stroke();

            while (true)
            {
                // Process Keyboards
                for (var i = 1; i < 11; i++)
                {
                    var isMonitoredKeyboard = IsMonitoredDevice(i) == 1;
                    var hasSubscription = false;
                    var hasContext = _contextCallbacks.ContainsKey(i);

                    while (ManagedWrapper.Receive(_deviceContext, i, ref stroke, 1) > 0)
                    {
                        var block = false;
                        if (isMonitoredKeyboard && _keyboardMappings.ContainsKey(i))
                        {
                            // Process Subscription Mode
                            var code = stroke.key.code;
                            var state = stroke.key.state;
                            if (state > 1)
                            {
                                code += 256;
                                state -= 2;
                            }

                            if (_keyboardMappings[i].ContainsKey(code))
                            {
                                hasSubscription = true;
                                var mapping = _keyboardMappings[i][code];
                                if (mapping.Block)
                                {
                                    block = true;
                                }

                                ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(1 - state));
                            }
                        }
                        // If the key was blocked by Subscription Mode, then move on to next key...
                        if (block) continue;

                        // If this key had no subscriptions, but Context Mode is set for this keyboard...
                        // ... then set the Context before sending the key
                        if (!hasSubscription && hasContext)
                        {
                            _contextCallbacks[i](1);
                        }

                        ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);

                        // If we are processing Context Mode, then Unset the context variable after sending the key
                        if (!hasSubscription && hasContext)
                        {
                            _contextCallbacks[i](0);
                        }
                    }
                }

                // Process Mice
                for (var i = 11; i < 21; i++)
                {
                    var isMontioredMouse = IsMonitoredDevice(i) == 1;
                    var hasSubscription = false;
                    var hasContext = _contextCallbacks.ContainsKey(i);

                    while (ManagedWrapper.Receive(_deviceContext, i, ref stroke, 1) > 0)
                    {
                        //Debug.WriteLine($"AHK| Mouse {i} seen - flags: {stroke.mouse.flags}, raw state: {stroke.mouse.state}");
                        var block = false;
                        if (isMontioredMouse)
                        {
                            if (stroke.mouse.state != 0 && _mouseButtonMappings.ContainsKey(i))
                            {
                                // Mouse Button
                                //Debug.WriteLine($"AHK| Mouse {i} seen - flags: {stroke.mouse.flags}, raw state: {stroke.mouse.state}");
                                var state = stroke.mouse.state;
                                // ToDo: Replace with Bit Shift, move into Helpers
                                var btn = 0;
                                while (state > 2)
                                {
                                    state /= 4;
                                    btn++;
                                };
                                if (_mouseButtonMappings[i].ContainsKey((ushort)btn))
                                {
                                    hasSubscription = true;
                                    var mapping = _mouseButtonMappings[i][(ushort)btn];
                                    if (mapping.Block)
                                    {
                                        block = true;
                                    }
                                    ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(2 - state));
                                }
                                //Debug.WriteLine($"AHK| Mouse {i} seen - button {btn}, state: {state}");
                            }
                            else if ((stroke.mouse.flags & (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute) ==
                                     (ushort) ManagedWrapper.MouseFlag.MouseMoveAbsolute
                                     && _mouseMoveAbsoluteMappings.ContainsKey(i))
                            {
                                // Absolute Mouse Move
                                hasSubscription = true;
                                var mapping = _mouseMoveAbsoluteMappings[i];
                                if (mapping.Block)
                                {
                                    block = true;
                                }

                                var x = stroke.mouse.x;
                                var y = stroke.mouse.y;
                                ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                            }
                            else if ((stroke.mouse.flags & (ushort)ManagedWrapper.MouseFlag.MouseMoveRelative) == 
                                     (ushort)ManagedWrapper.MouseFlag.MouseMoveRelative
                                     && _mouseMoveRelativeMappings.ContainsKey(i))
                            {
                                // Relative Mouse Move
                                hasSubscription = true;
                                var mapping = _mouseMoveRelativeMappings[i];
                                if (mapping.Block)
                                {
                                    block = true;
                                }

                                var x = stroke.mouse.x;
                                var y = stroke.mouse.y;
                                ThreadPool.QueueUserWorkItem(threadProc => mapping.Callback(x, y));
                            }
                        }
                        // If this key had no subscriptions, but Context Mode is set for this mouse...
                        // ... then set the Context before sending the button
                        if (!hasSubscription && hasContext)
                        {
                            // Set Context
                            _contextCallbacks[i](1);
                        }
                        if (!(block))
                        {
                            ManagedWrapper.Send(_deviceContext, i, ref stroke, 1);
                        }
                        // If we are processing Context Mode, then Unset the context variable after sending the button
                        if (!hasSubscription && hasContext)
                        {
                            // Unset Context
                            _contextCallbacks[i](0);
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

        private class MappingOptions
        {
            public bool Block { get; set; } = false;
            public dynamic Callback { get; set; }
        }
        #endregion


        public void Dispose()
        {
            SetThreadState(false);
        }

    }


}
