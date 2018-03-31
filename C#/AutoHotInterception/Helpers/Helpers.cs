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
            //return id >= start && id <= end;
        }

        public static void GetVidPid(string str, ref int vid, ref int pid)
        {
            MatchCollection matches = Regex.Matches(str, @"VID_(\w{4})&PID_(\w{4})");
            if ((matches.Count > 0) && (matches[0].Groups.Count > 1))
            {
                vid = Convert.ToInt32(matches[0].Groups[1].Value, 16);
                pid = Convert.ToInt32(matches[0].Groups[2].Value, 16);
            }
        }

        public static DeviceInfo[] GetDeviceList(IntPtr deviceContext)
        {
            var ret = new List<DeviceInfo>();
            for (var i = 1; i < 21; i++)
            {
                var handle = ManagedWrapper.GetHardwareStr(deviceContext, i, 1000);
                if (handle == "") continue;
                int foundVid = 0, foundPid = 0;
                HelperFunctions.GetVidPid(handle, ref foundVid, ref foundPid);
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
        public static ushort ButtonAndStateToStrokeState(int btn, int state)
        {
            var bit = btn * 2;
            if (state == 0)
                bit += 1;
            return (ushort)(1 << bit);
        }

        public class DeviceInfo
        {
            public int Id { get; set; }
            public bool IsMouse { get; set; } = false;
            public int Vid { get; set; }
            public int Pid { get; set; }
        }

    }
}
