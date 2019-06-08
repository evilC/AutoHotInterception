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

        private static readonly Dictionary<int, ButtonState> ButtonStateLookupTable = new Dictionary<int, ButtonState>()
        {
            { 1, new ButtonState{Button = 0, State = 1} },
            { 2, new ButtonState{Button = 0, State = 0} },
            { 4, new ButtonState{Button = 1, State = 1} },
            { 8, new ButtonState{Button = 1, State = 0} },
            { 16, new ButtonState{Button = 2, State = 1} },
            { 32, new ButtonState{Button = 2, State = 0} },
            { 64, new ButtonState{Button = 3, State = 1} },
            { 128, new ButtonState{Button = 3, State = 0} },
            { 256, new ButtonState{Button = 4, State = 1} },
            { 512, new ButtonState{Button = 4, State = 0} },
        };

        public static ButtonState[] MouseStrokeToButtonStates(ManagedWrapper.Stroke stroke)
        {
            int state = stroke.mouse.state;

            // Buttons
            var buttonStates = new List<ButtonState>();
            foreach (var buttonState in ButtonStateLookupTable)
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
                        State = (stroke.mouse.rolling < 0 ? -1 : 1)
                    }
                );
            }
            else if ((state & 0x800) == 0x800) // Wheel left / right
            {
                buttonStates.Add(
                    new ButtonState
                    {
                        Button = 6,
                        State = (stroke.mouse.rolling < 0 ? -1 : 1)
                    }
                );
            }
            return buttonStates.ToArray();
        }

        public static KeyboardState KeyboardStrokeToKeyboardState(ManagedWrapper.Stroke stroke)
        {
            var code = stroke.key.code;
            var state = stroke.key.state;
            var retVal = new KeyboardState();
            if (code == 54) code = 310;

            // If state is shifted up by 2 (1 or 2 instead of 0 or 1), then this is an "Extended" key code
            if (state > 1)
            {
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
        }

        public class KeyboardState
        {
            public ushort Code { get; set; }
            public ushort State { get; set; }
            public bool Ignore { get; set; }
        }
    }
}