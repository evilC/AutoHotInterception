using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AutoHotInterception.Helpers
{
    public static class HelperFunctions
    {
        public static void IsValidDeviceId(bool isMouse, int id)
        {
            var start = isMouse ? 11 : 1;
            var end = start + 9;
            if (id < start || id > end)
                throw new ArgumentOutOfRangeException(nameof(id),
                    $"Invalid id ID: {id} for device type {(isMouse ? "Mouse" : "Keyboard")}. Device IDs for this type should be between {start} and {end}");
        }

        public static void GetVidPid(string str, ref int vid, ref int pid)
        {
            var matches = Regex.Matches(str, @"VID_(\w{4})&PID_(\w{4})");
            if (matches.Count <= 0 || matches[0].Groups.Count <= 1) return;
            vid = Convert.ToInt32(matches[0].Groups[1].Value, 16);
            pid = Convert.ToInt32(matches[0].Groups[2].Value, 16);
        }

        #region Device Querying
        /// <summary>
        ///     Tries to get Device ID from VID/PID
        /// </summary>
        /// <param name="deviceContext">The Interception device context</param>
        /// <param name="isMouse">Whether the device is a mouse or a keyboard</param>
        /// <param name="vid">The VID of the device</param>
        /// <param name="pid">The PID of the device</param>
        /// <param name="instance">The instance of the VID/PID (Optional)</param>
        /// <returns></returns>
        public static int GetDeviceId(IntPtr deviceContext, bool isMouse, int vid, int pid, int instance = 1)
        {
            var start = isMouse ? 11 : 0;
            var max = isMouse ? 21 : 11;
            for (var i = start; i < max; i++)
            {
                var hardwareStr = ManagedWrapper.GetHardwareStr(deviceContext, i, 1000);
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
        /// <param name="deviceContext">The Interception device context</param>
        /// <param name="isMouse">Whether the device is a mouse or a keyboard</param>
        /// <param name="handle">The Hardware String (handle) of the device</param>
        /// <param name="instance">The instance of the VID/PID (Optional)</param>
        /// <returns></returns>
        public static int GetDeviceIdFromHandle(IntPtr deviceContext, bool isMouse, string handle, int instance = 1)
        {
            var start = isMouse ? 11 : 0;
            var max = isMouse ? 21 : 11;
            for (var i = start; i < max; i++)
            {
                var hardwareStr = ManagedWrapper.GetHardwareStr(deviceContext, i, 1000);
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
        public static DeviceInfo[] GetDeviceList(IntPtr deviceContext)
        {
            var ret = new List<DeviceInfo>();
            for (var i = 1; i < 21; i++)
            {
                var handle = ManagedWrapper.GetHardwareStr(deviceContext, i, 1000);
                if (handle == "") continue;
                int foundVid = 0, foundPid = 0;
                GetVidPid(handle, ref foundVid, ref foundPid);
                //if (foundVid == 0 || foundPid == 0) continue;

                ret.Add(new DeviceInfo {Id = i, Vid = foundVid, Pid = foundPid, IsMouse = i > 10, Handle = handle});
            }

            return ret.ToArray();
        }
        #endregion

        /// <summary>
        ///     Converts a button index plus a state into a State value for a mouse Stroke
        /// </summary>
        /// <param name="btn">0 = LMB, 1 = RMB etc</param>
        /// <param name="state">1 = Press, 0 = Release</param>
        /// <returns>A State value for a Mouse Stroke</returns>
        public static ManagedWrapper.Stroke MouseButtonAndStateToStroke(int btn, int state)
        {
            var stroke = new ManagedWrapper.Stroke();
            var power = btn < 5 ? btn * 2 + (state == 0 ? 1 : 0) : btn + 5;
            stroke.mouse.state = (ushort) (1 << power);
            if (btn >= 5) stroke.mouse.rolling = (short) (state * 120);
            return stroke;
        }

        private static readonly Dictionary<int, ButtonState> StrokeFlagToButtonState = new Dictionary<int, ButtonState>()
        {
            { 1, new ButtonState{Button = 0, State = 1, Flag = 1} },        // LMB Press
            { 2, new ButtonState{Button = 0, State = 0, Flag = 2} },        // LMB Release
            { 4, new ButtonState{Button = 1, State = 1, Flag = 4} },        // RMB Press
            { 8, new ButtonState{Button = 1, State = 0, Flag = 8} },        // RMB Release
            { 16, new ButtonState{Button = 2, State = 1, Flag = 16} },      // MMB Press
            { 32, new ButtonState{Button = 2, State = 0, Flag = 32} },      // MMB Release
            { 64, new ButtonState{Button = 3, State = 1, Flag = 64} },      // XB1 Press
            { 128, new ButtonState{Button = 3, State = 0, Flag = 128} },    // XB1 Release
            { 256, new ButtonState{Button = 4, State = 1, Flag = 256} },    // XB2 Press
            { 512, new ButtonState{Button = 4, State = 0, Flag = 512} }     // XB2 Release
        };

        public static ButtonState[] MouseStrokeToButtonStates(ManagedWrapper.Stroke stroke)
        {
            int state = stroke.mouse.state;

            // Buttons
            var buttonStates = new List<ButtonState>();
            foreach (var buttonState in StrokeFlagToButtonState)
            {
                if (state < buttonState.Key) break;
                if ((state & buttonState.Key) != buttonState.Key) continue;

                buttonStates.Add(buttonState.Value);
                state -= buttonState.Key;
            }

            // Wheel
            if ((state & 0x400) == 0x400) // Wheel up / down
            {
                buttonStates.Add(
                    new ButtonState
                    {
                        Button = 5,
                        State = (stroke.mouse.rolling < 0 ? -1 : 1),
                        Flag = 0x400
                    }
                );
            }
            else if ((state & 0x800) == 0x800) // Wheel left / right
            {
                buttonStates.Add(
                    new ButtonState
                    {
                        Button = 6,
                        State = (stroke.mouse.rolling < 0 ? -1 : 1),
                        Flag = 0x800
                    }
                );
            }
            return buttonStates.ToArray();
        }

        public static KeyboardState KeyboardStrokeToKeyboardState(ManagedWrapper.Stroke stroke, bool lct)
        {
            var code = stroke.key.code;
            var state = stroke.key.state;
            var retVal = new KeyboardState {ChangeLctrl = 0};
            if (code == 54 /* Right Shift */ || code == 69 /* NumLock */) code += 256;

            // If state is shifted up by 2 (1 or 2 instead of 0 or 1), then this is an "Extended" key code
            if (state > 1)
            {
                if (code == 29 && state == 4)
                {
                    retVal.ChangeLctrl = 1;
                }
                if (code == 42 || state > 3)
                {
                    // code == 42
                    // Shift (42/0x2a) with extended flag = the key after this one is extended.
                    // Example case is Delete (The one above the arrow keys, not on numpad)...
                    // ... this generates a stroke of 0x2a (Shift) with *extended flag set* (Normal shift does not do this)...
                    // ... followed by 0x53 with extended flag set.
                    // We do not want to fire subsriptions for the extended shift, but *do* want to let the key flow through...
                    // ... so that is handled here.
                    // When the extended key (Delete in the above example) subsequently comes through...
                    // ... it will have code 0x53, which we shift to 0x153 (Adding 256 Dec) to signify extended version...
                    // ... as this is how AHK behaves with GetKeySC()

                    // state > 3
                    // Pause sends code 69 normally as state 0/1, but also LCtrl (29) as state 4/5
                    // Ignore the LCtrl in this case

                    // Set flag to indicate ignore
                    retVal.Ignore = true;
                }
                else
                {
                    // Extended flag set
                    // Shift code up by 256 (0x100) to signify extended code
                    code += 256;
                    state -= 2;
                }
            }

            if (code == 325 && lct == true)
            {
                if (state == 1)
                {
                    retVal.ChangeLctrl = 2;
                }
                code = 69;
            }

            retVal.Code = code;
            retVal.State = (ushort) (1 - state);
            return retVal;
        }

        public class DeviceInfo
        {
            public int Id { get; set; }
            public bool IsMouse { get; set; }
            public int Vid { get; set; }
            public int Pid { get; set; }
            public string Handle { get; set; }
        }

        public class ButtonState
        {
            public ushort Button { get; set; }
            public int State { get; set; }
            public ushort Flag { get; set; } // Preserve original flag, so it can be removed from stroke
        }

        public class KeyboardState
        {
            public ushort Code { get; set; }
            public ushort State { get; set; }
            public bool Ignore { get; set; }
            public int ChangeLctrl { get; set; }
        }
    }
}
