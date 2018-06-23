using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoHotInterception.Helpers
{
    public static class HelperFunctions
    {
        public static void IsValidDeviceId(bool isMouse, int id)
        {
            var start = isMouse ? 11 : 1;
            var end = start + 9;
            if (id < start || id > end)
            {
                throw new ArgumentOutOfRangeException(nameof(id), $"Invalid id ID: {id} for device type {(isMouse ? "Mouse" : "Keyboard")}. Device IDs for this type should be between {start} and {end}");
            }
        }

        public static void GetVidPid(string str, ref int vid, ref int pid)
        {
            var matches = Regex.Matches(str, @"VID_(\w{4})&PID_(\w{4})");
            if ((matches.Count <= 0) || (matches[0].Groups.Count <= 1)) return;
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
                if (foundVid == 0 || foundPid == 0) continue;

                ret.Add(new DeviceInfo { Id = i, Vid = foundVid, Pid = foundPid, IsMouse = i > 10 });
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Converts a button index plus a state into a State value for a mouse Stroke
        /// </summary>
        /// <param name="btn">0 = LMB, 1 = RMB etc</param>
        /// <param name="state">1 = Press, 0 = Release</param>
        /// <returns>A State value for a Mouse Stroke</returns>
        public static ManagedWrapper.Stroke ButtonAndStateToStroke(int btn, int state)
        {
            var stroke = new ManagedWrapper.Stroke();
            var power = btn < 5 ? btn * 2 + (state == 0 ? 1 : 0) : btn + 5;
            stroke.mouse.state = (ushort)(1 << power);
            if (btn >= 5) stroke.mouse.rolling = (short)(state * 120);
            return stroke;
        }

        public static ButtonState StrokeStateToButtonState(ManagedWrapper.Stroke stroke)
        {
            int state = stroke.mouse.state;
            ushort btn = 0;
            if (state < 0x400)
            {
                while (state > 2)
                {
                    state >>= 2;
                    btn++;
                }
                state = 2 - state; // 1 = Pressed, 0 = Released
            }
            else
            {
                if (state == 0x400) btn = 5; // Vertical mouse wheel
                else if (state == 0x800) btn = 6; // Horizontal mouse wheel
                state = stroke.mouse.rolling < 0 ? -1 : 1;
            }
            return new ButtonState {Button = btn, State = state};
        }

        public class DeviceInfo
        {
            public int Id { get; set; }
            public bool IsMouse { get; set; }
            public int Vid { get; set; }
            public int Pid { get; set; }
        }

        public class ButtonState
        {
            public ushort Button { get; set; }
            public int State { get; set; }
        }
    }
}
