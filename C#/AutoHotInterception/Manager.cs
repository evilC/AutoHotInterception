using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        /// Subscribes to Absolute mouse movement
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

            var device = (MouseHandler)DeviceHandlers[id];
            device.SubscribeMouseMoveAbsolute(new MappingOptions
            { Block = block, Concurrent = concurrent, Callback = callback });

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Unsubscribes from absolute mouse movement
        /// </summary>
        /// <param name="id">The id of the mouse</param>
        public void UnsubscribeMouseMoveAbsolute(int id)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            var device = (MouseHandler)DeviceHandlers[id];
            device.UnsubscribeMouseMoveAbsolute();

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
            SetFilterState(false);

            var device = (MouseHandler)DeviceHandlers[id];
            device.SubscribeMouseMoveRelative(new MappingOptions
            { Block = block, Concurrent = concurrent, Callback = callback });

            SetFilterState(true);
            SetThreadState(true);
        }

        /// <summary>
        /// Unsubscribes from relative mouse movement
        /// </summary>
        /// <param name="id">The id of the mouse</param>
        public void UnsubscribeMouseMoveRelative(int id)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            SetFilterState(false);

            var device = (MouseHandler)DeviceHandlers[id];
            device.UnsubscribeMouseMoveRelative();

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
        /// Sends a keyboard key event
        /// </summary>
        /// <param name="id">The ID of the Keyboard to send as</param>
        /// <param name="code">The ScanCode to send</param>
        /// <param name="state">The State to send (1 = pressed, 0 = released)</param>
        public void SendKeyEvent(int id, ushort code, int state)
        {
            HelperFunctions.IsValidDeviceId(false, id);
            var device = (KeyboardHandler)DeviceHandlers[id];
            device.SendKeyEvent(code, state);
        }

        /// <summary>
        /// Sends Mouse button events
        /// </summary>
        /// <param name="id"></param>
        /// <param name="btn">Button ID to send</param>
        /// <param name="state">State of the button</param>
        /// <returns></returns>
        public void SendMouseButtonEvent(int id, int btn, int state)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            var device = (MouseHandler)DeviceHandlers[id];
            device.SendMouseButtonEvent(btn, state);
        }

        /// <summary>
        /// Same as <see cref="SendMouseButtonEvent" />, but sends button events in Absolute mode (with coordinates)
        /// </summary>
        /// <param name="id">ID of the mouse</param>
        /// <param name="btn">Button ID to send</param>
        /// <param name="state">State of the button</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        public void SendMouseButtonEventAbsolute(int id, int btn, int state, int x, int y)
        {
            HelperFunctions.IsValidDeviceId(true, id);
            var device = (MouseHandler)DeviceHandlers[id];
            device.SendMouseButtonEventAbsolute(btn, state, x, y);
        }

        public void SendMouseMove(int id, int x, int y)
        {
            SendMouseMoveRelative(id, x, y);
        }

        /// <summary>
        ///     Sends Relative Mouse Movement
        /// </summary>
        /// <param name="id">The id of the mouse</param>
        /// <param name="x">X movement</param>
        /// <param name="y">Y movement</param>
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
            var device = (MouseHandler)DeviceHandlers[id];

            device.SendMouseMoveAbsolute(x, y);
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
            return DeviceHandlers[device].IsFiltered();
        }

        private void SetFilterState(bool state)
        {
            ManagedWrapper.SetFilter(DeviceContext, IsMonitoredDevice,
                state ? ManagedWrapper.Filter.All : ManagedWrapper.Filter.None);
        }

        private static void PollThread(object obj)
        {
            var token = (CancellationToken)obj;
            //Debug.WriteLine($"AHK| Poll Thread Started");
            _pollThreadRunning = true;
            var stroke = new ManagedWrapper.Stroke();
            int deviceId;
            while (!token.IsCancellationRequested)
            {
                //var strokes = new List<ManagedWrapper.Stroke>();
                var deviceStrokes = new Dictionary<int, List<ManagedWrapper.Stroke>>();
                // While no input happens, this loop will exit every 1ms to allow us to check if cancellation has been requested
                // WaitWithTimeout is used with a timeout of 10ms instead of Wait, so that when we eg use SetState to turn the thread off...
                // ... any input which was filtered and is waiting to be processed can be processed (eg lots of mouse moves buffered)
                while (ManagedWrapper.Receive(DeviceContext, deviceId = ManagedWrapper.WaitWithTimeout(DeviceContext, 0), ref stroke, 1) > 0)
                {
                    if (!deviceStrokes.ContainsKey(deviceId))
                    {
                        deviceStrokes.Add(deviceId, new List<ManagedWrapper.Stroke>());
                    }
                    deviceStrokes[deviceId].Add(stroke);
                    //DeviceHandlers[i].ProcessStroke(stroke);
                }
                if (deviceStrokes.Count == 0) continue;
                foreach (var strokes in deviceStrokes)
                {
                    //var keyEvents = new List<KeyEvent>();
                    //foreach (var s in strokes.Value)
                    //{
                    //    keyEvents.Add(new KeyEvent { Code = s.key.code, State = s.key.state });
                    //}
                    DeviceHandlers[strokes.Key].ProcessStroke(strokes.Value);
                }
            }
            _pollThreadRunning = false;
            //Debug.WriteLine($"AHK| Poll Thread Ended");
        }
        #endregion
    }
}