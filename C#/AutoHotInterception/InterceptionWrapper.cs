using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class InterceptionWrapper
{
    private readonly IntPtr _deviceContext;
    private Thread _watcherThread;
    private bool _watcherThreadRunning = false;
    private readonly bool _filterState = false;

    private readonly Dictionary<int, List<Mapping>> _mappings = new Dictionary<int, List<Mapping>>();

    public InterceptionWrapper()
    {
        _deviceContext = CreateContext();
    }

    public string Test()
    {
        return "OK";
    }

    public string GetDeviceList()
    {
        var str = "Devices:\n";
        for (var i = 1; i < 11; i++)
        {
            var handle = GetHardwareStr(_deviceContext, i, 1000);
            int foundVid = 0, foundPid = 0;
            GetVidPid(handle, ref foundVid, ref foundPid);
            if (foundVid == 0 || foundPid == 0)
            {
                break;
            }
            str += $"ID: {i}, VID: 0x{foundVid:X}, PID: 0x{foundPid:X}\n";
        }

        return str;
    }

    public bool SubscribeKey(uint code, bool block, dynamic callback, int vid = 0, int pid = 0)
    {
        SetFilterState(false);
        var id = 0;
        if (vid != 0 && pid != 0)
        {
            id = GetDeviceId(vid, pid);
        }

        if (id == 0) return false;
        if (!_mappings.ContainsKey(id))
        {
            _mappings.Add(id, new List<Mapping>());
        }
        _mappings[id].Add(new Mapping() { code = Convert.ToUInt16(code), block = block, callback = callback });
        SetFilterState(true);
        if (!_watcherThreadRunning)
        {
            _watcherThreadRunning = true;
            _watcherThread = new Thread(WatcherThread);
            _watcherThread.Start();
        }
        return true;
    }

    ~InterceptionWrapper()
    {
        DestroyContext(_deviceContext);
    }

    public int GetDeviceId(int vid, int pid)
    {
        for (var i = 1; i < 11; i++)
        {
            var handle = GetHardwareStr(_deviceContext, i, 1000);
            int foundVid = 0, foundPid = 0;
            GetVidPid(handle, ref foundVid, ref foundPid);
            if (foundVid == vid && foundPid == pid)
            {
                return i;
            }
        }

        return 0;
    }

    private static void GetVidPid(string str, ref int vid, ref int pid)
    {
        MatchCollection matches = Regex.Matches(str, @"VID_(\w{4})&PID_(\w{4})");
        if ((matches.Count > 0) && (matches[0].Groups.Count > 1))
        {
            vid = Convert.ToInt32(matches[0].Groups[1].Value, 16);
            pid = Convert.ToInt32(matches[0].Groups[2].Value, 16);
        }
    }

    private int IsMonitoredKeyboard(int device)
    {
        return Convert.ToInt32(_mappings.ContainsKey(device));
    }

    private void SetFilterState(bool state)
    {
        if (state && !_filterState)
        {
            SetFilter(_deviceContext, IsMonitoredKeyboard, Filter.All);
        }
        else if (!state && _filterState)
        {
            SetFilter(_deviceContext, IsMonitoredKeyboard, Filter.None);
        }
    }

    private void WatcherThread()
    {
        Stroke stroke = new Stroke();
        //int device = Wait(deviceContext);

        while (true)
        {
            for (var i = 1; i < 11; i++)
            {
                var isMonitoredKeyboard = _mappings.ContainsKey(i);

                while (Receive(_deviceContext, i, ref stroke, 1) > 0)
                {
                    var block = false;
                    if (isMonitoredKeyboard)
                    {
                        foreach (var mapping in _mappings[i])
                        {
                            if (stroke.key.code != mapping.code) continue;
                            if (mapping.block)
                            {
                                block = true;
                            }
                            mapping.callback(1 - stroke.key.state);
                            break;
                        }
                    }
                    if (!block)
                    {
                        Send(_deviceContext, i, ref stroke, 1);
                    }
                }
            }
            Thread.Sleep(10);
        }
    }

    private class Mapping
    {
        public ushort code;
        public bool block = false;
        public dynamic callback;
    }


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int Predicate(int device);
    /*
    typedef void *InterceptionContext;
    typedef int InterceptionDevice;
    typedef int InterceptionPrecedence;
    typedef unsigned short InterceptionFilter;
    typedef int (*InterceptionPredicate)(InterceptionDevice device);
    */

    [Flags]
    public enum KeyState
    {
        Down = 0x00,
        Up = 0x01,
        E0 = 0x02,
        E1 = 0x04,
        TermsrvSetLED = 0x08,
        TermsrvShadow = 0x10,
        TermsrvVKPacket = 0x20
        /*
        enum InterceptionKeyState
        INTERCEPTION_KEY_DOWN = 0x00,
        INTERCEPTION_KEY_UP = 0x01,
        INTERCEPTION_KEY_E0 = 0x02,
        INTERCEPTION_KEY_E1 = 0x04,
        INTERCEPTION_KEY_TERMSRV_SET_LED = 0x08,
        INTERCEPTION_KEY_TERMSRV_SHADOW = 0x10,
        INTERCEPTION_KEY_TERMSRV_VKPACKET = 0x20
        */
    }

    [Flags]
    public enum MouseState
    {
        LeftButtonDown = 0x001,
        LeftButtonUp = 0x002,
        RightButtonDown = 0x004,
        RightButtonUp = 0x008,
        MiddleButtonDown = 0x010,
        MiddleButtonUp = 0x020,

        Button1Down = LeftButtonDown,
        Button1Up = LeftButtonUp,
        Button2Down = RightButtonDown,
        Button2Up = RightButtonUp,
        Button3Down = MiddleButtonDown,
        Button3Up = MiddleButtonUp,

        Button4Down = 0x040,
        Button4Up = 0x080,
        Button5Down = 0x100,
        Button5Up = 0x200,

        Wheel = 0x400,
        HWheel = 0x800
        /*
        enum InterceptionMouseState
        {
        INTERCEPTION_MOUSE_LEFT_BUTTON_DOWN = 0x001,
        INTERCEPTION_MOUSE_LEFT_BUTTON_UP = 0x002,
        INTERCEPTION_MOUSE_RIGHT_BUTTON_DOWN = 0x004,
        INTERCEPTION_MOUSE_RIGHT_BUTTON_UP = 0x008,
        INTERCEPTION_MOUSE_MIDDLE_BUTTON_DOWN = 0x010,
        INTERCEPTION_MOUSE_MIDDLE_BUTTON_UP = 0x020,

        INTERCEPTION_MOUSE_BUTTON_1_DOWN = INTERCEPTION_MOUSE_LEFT_BUTTON_DOWN,
        INTERCEPTION_MOUSE_BUTTON_1_UP = INTERCEPTION_MOUSE_LEFT_BUTTON_UP,
        INTERCEPTION_MOUSE_BUTTON_2_DOWN = INTERCEPTION_MOUSE_RIGHT_BUTTON_DOWN,
        INTERCEPTION_MOUSE_BUTTON_2_UP = INTERCEPTION_MOUSE_RIGHT_BUTTON_UP,
        INTERCEPTION_MOUSE_BUTTON_3_DOWN = INTERCEPTION_MOUSE_MIDDLE_BUTTON_DOWN,
        INTERCEPTION_MOUSE_BUTTON_3_UP = INTERCEPTION_MOUSE_MIDDLE_BUTTON_UP,

        INTERCEPTION_MOUSE_BUTTON_4_DOWN = 0x040,
        INTERCEPTION_MOUSE_BUTTON_4_UP = 0x080,
        INTERCEPTION_MOUSE_BUTTON_5_DOWN = 0x100,
        INTERCEPTION_MOUSE_BUTTON_5_UP = 0x200,

        INTERCEPTION_MOUSE_WHEEL = 0x400,
        INTERCEPTION_MOUSE_HWHEEL = 0x800
        };
        */
    }

    [Flags]
    public enum Filter : ushort
    {
        None = 0x0000,
        All = 0xFFFF,
        KeyDown = KeyState.Up,
        KeyUp = KeyState.Up << 1,
        KeyE0 = KeyState.E0 << 1,
        KeyE1 = KeyState.E1 << 1,
        KeyTermsrvSetLED = KeyState.TermsrvSetLED << 1,
        KeyTermsrvShadow = KeyState.TermsrvShadow << 1,
        KeyTermsrvVKPacket = KeyState.TermsrvVKPacket << 1,
        /*
        enum InterceptionFilterKeyState
        INTERCEPTION_FILTER_KEY_NONE = 0x0000,
        INTERCEPTION_FILTER_KEY_ALL = 0xFFFF,
        INTERCEPTION_FILTER_KEY_DOWN = INTERCEPTION_KEY_UP,
        INTERCEPTION_FILTER_KEY_UP = INTERCEPTION_KEY_UP << 1,
        INTERCEPTION_FILTER_KEY_E0 = INTERCEPTION_KEY_E0 << 1,
        INTERCEPTION_FILTER_KEY_E1 = INTERCEPTION_KEY_E1 << 1,
        INTERCEPTION_FILTER_KEY_TERMSRV_SET_LED = INTERCEPTION_KEY_TERMSRV_SET_LED << 1,
        INTERCEPTION_FILTER_KEY_TERMSRV_SHADOW = INTERCEPTION_KEY_TERMSRV_SHADOW << 1,
        INTERCEPTION_FILTER_KEY_TERMSRV_VKPACKET = INTERCEPTION_KEY_TERMSRV_VKPACKET << 1
        */

        // enum InterceptionFilterMouseState
        //MOUSE_NONE = 0x0000,
        //MOUSE_ALL = 0xFFFF,
        MouseMove = 0x1000,

        MouseLeftButtonDown = MouseState.LeftButtonDown,
        MouseLeftButtonUp = MouseState.LeftButtonUp,
        MouseRightButtonDown = MouseState.RightButtonDown,
        MouseRightButtonUp = MouseState.RightButtonUp,
        MouseMiddleButtonDown = MouseState.MiddleButtonDown,
        MouseMiddleButtonUp = MouseState.MiddleButtonUp,

        MouseButton1Down = MouseState.Button1Down,
        MouseButton1Up = MouseState.Button1Up,
        MouseButton2Down = MouseState.Button2Down,
        MouseButton2Up = MouseState.Button2Up,
        MouseButton3Down = MouseState.Button3Down,
        MouseButton3Up = MouseState.Button3Up,

        MouseButton4Down = MouseState.Button4Down,
        MouseButton4Up = MouseState.Button4Up,
        MouseButton5Down = MouseState.Button5Down,
        MouseButton5Up = MouseState.Button5Up,
        MouseButtonAnyDown = MouseState.Button1Down | MouseState.Button2Down | MouseState.Button3Down | MouseState.Button4Down | MouseState.Button5Down,
        MouseButtonAnyUp = MouseState.Button1Up | MouseState.Button2Up | MouseState.Button3Up | MouseState.Button4Up | MouseState.Button5Up,

        MouseWheel = MouseState.Wheel,
        MouseHWheel = MouseState.HWheel
        /*
        enum InterceptionFilterMouseState
        {
        INTERCEPTION_FILTER_MOUSE_NONE = 0x0000,
        INTERCEPTION_FILTER_MOUSE_ALL = 0xFFFF,

        INTERCEPTION_FILTER_MOUSE_LEFT_BUTTON_DOWN = INTERCEPTION_MOUSE_LEFT_BUTTON_DOWN,
        INTERCEPTION_FILTER_MOUSE_LEFT_BUTTON_UP = INTERCEPTION_MOUSE_LEFT_BUTTON_UP,
        INTERCEPTION_FILTER_MOUSE_RIGHT_BUTTON_DOWN = INTERCEPTION_MOUSE_RIGHT_BUTTON_DOWN,
        INTERCEPTION_FILTER_MOUSE_RIGHT_BUTTON_UP = INTERCEPTION_MOUSE_RIGHT_BUTTON_UP,
        INTERCEPTION_FILTER_MOUSE_MIDDLE_BUTTON_DOWN = INTERCEPTION_MOUSE_MIDDLE_BUTTON_DOWN,
        INTERCEPTION_FILTER_MOUSE_MIDDLE_BUTTON_UP = INTERCEPTION_MOUSE_MIDDLE_BUTTON_UP,

        INTERCEPTION_FILTER_MOUSE_BUTTON_1_DOWN = INTERCEPTION_MOUSE_BUTTON_1_DOWN,
        INTERCEPTION_FILTER_MOUSE_BUTTON_1_UP = INTERCEPTION_MOUSE_BUTTON_1_UP,
        INTERCEPTION_FILTER_MOUSE_BUTTON_2_DOWN = INTERCEPTION_MOUSE_BUTTON_2_DOWN,
        INTERCEPTION_FILTER_MOUSE_BUTTON_2_UP = INTERCEPTION_MOUSE_BUTTON_2_UP,
        INTERCEPTION_FILTER_MOUSE_BUTTON_3_DOWN = INTERCEPTION_MOUSE_BUTTON_3_DOWN,
        INTERCEPTION_FILTER_MOUSE_BUTTON_3_UP = INTERCEPTION_MOUSE_BUTTON_3_UP,

        INTERCEPTION_FILTER_MOUSE_BUTTON_4_DOWN = INTERCEPTION_MOUSE_BUTTON_4_DOWN,
        INTERCEPTION_FILTER_MOUSE_BUTTON_4_UP = INTERCEPTION_MOUSE_BUTTON_4_UP,
        INTERCEPTION_FILTER_MOUSE_BUTTON_5_DOWN = INTERCEPTION_MOUSE_BUTTON_5_DOWN,
        INTERCEPTION_FILTER_MOUSE_BUTTON_5_UP = INTERCEPTION_MOUSE_BUTTON_5_UP,

        INTERCEPTION_FILTER_MOUSE_WHEEL = INTERCEPTION_MOUSE_WHEEL,
        INTERCEPTION_FILTER_MOUSE_HWHEEL = INTERCEPTION_MOUSE_HWHEEL,

        INTERCEPTION_FILTER_MOUSE_MOVE = 0x1000
        };
        */
    }

    [Flags]
    public enum MouseFlag : ushort
    {
        MouseMoveRelative = 0x000,
        MouseMoveAbsolute = 0x001,
        MouseVirturalDesktop = 0x002,
        MouseAttributesChanged = 0x004,
        MouseMoveNocoalesce = 0x008,
        MouseTermsrvSrcShadow = 0x100
        /*
        enum InterceptionMouseFlag
        {
        INTERCEPTION_MOUSE_MOVE_RELATIVE = 0x000,
        INTERCEPTION_MOUSE_MOVE_ABSOLUTE = 0x001,
        INTERCEPTION_MOUSE_VIRTUAL_DESKTOP = 0x002,
        INTERCEPTION_MOUSE_ATTRIBUTES_CHANGED = 0x004,
        INTERCEPTION_MOUSE_MOVE_NOCOALESCE = 0x008,
        INTERCEPTION_MOUSE_TERMSRV_SRC_SHADOW = 0x100
        };
        */
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MouseStroke
    {
        public ushort state;
        public ushort flags;
        public short rolling;
        public int x;
        public int y;
        public uint information;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyStroke
    {
        public ushort code;
        public ushort state;
        public uint information;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Stroke
    {
        [FieldOffset(0)]
        public MouseStroke mouse;

        [FieldOffset(0)]
        public KeyStroke key;
    }


    [DllImport("interception.dll", EntryPoint = "interception_create_context", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateContext();

    [DllImport("interception.dll", EntryPoint = "interception_destroy_context", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyContext(IntPtr context);

    [DllImport("interception.dll", EntryPoint = "interception_set_filter", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetFilter(IntPtr context, Predicate predicate, Filter filter);
    // public static extern void SetFilter(IntPtr context, Predicate predicate, ushort filter);

    // InterceptionFilter INTERCEPTION_API interception_get_filter(InterceptionContext context, InterceptionDevice device);
    [DllImport("interception.dll", EntryPoint = "interception_get_filter", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort GetFilter(IntPtr context, int device);

    [DllImport("interception.dll", EntryPoint = "interception_receive", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Receive(IntPtr context, int device, ref Stroke stroke, uint nstroke);

    [DllImport("interception.dll", EntryPoint = "interception_send", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Send(IntPtr context, int device, ref Stroke stroke, uint nstroke);

    [DllImport("interception.dll", EntryPoint = "interception_is_keyboard", CallingConvention = CallingConvention.Cdecl)]
    public static extern int IsKeyboard(int device);

    [DllImport("interception.dll", EntryPoint = "interception_is_mouse", CallingConvention = CallingConvention.Cdecl)]
    public static extern int IsMouse(int device);

    [DllImport("interception.dll", EntryPoint = "interception_is_invalid", CallingConvention = CallingConvention.Cdecl)]
    public static extern int IsInvalid(int device);

    //InterceptionDevice INTERCEPTION_API interception_wait(InterceptionContext context);
    [DllImport("interception.dll", EntryPoint = "interception_wait", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Wait(IntPtr context);

    //InterceptionDevice INTERCEPTION_API interception_wait_with_timeout(InterceptionContext context, unsigned long milliseconds);
    [DllImport("interception.dll", EntryPoint = "interception_wait_with_timeout", CallingConvention = CallingConvention.Cdecl)]
    public static extern int WaitWithTimeout(int device, ulong milliseconds);

    // unsigned int INTERCEPTION_API interception_get_hardware_id(InterceptionContext context, InterceptionDevice device, void *hardware_id_buffer, unsigned int buffer_size);
    [DllImport("interception.dll", EntryPoint = "interception_get_hardware_id", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetHardwareID(IntPtr context, int device, IntPtr hardwareidbuffer, uint buffersize);
    // public static extern uint GetHardwareID(IntPtr context, int device, [MarshalAs(UnmanagedType.ByValArray,SizeConst=500)]char[] hardwareidbuffer, uint buffersize);
    //public static extern uint GetHardwareID(IntPtr context, int device, ref _wchar_t[] hardwareidbuffer, uint buffersize);

    public static string GetHardwareStr(IntPtr context, int device, int chars = 0)
    {
        if (chars == 0)
            chars = 500;
        String result = "";
        IntPtr bufferptr = Marshal.StringToHGlobalUni(new string(new char[chars]));
        uint length = GetHardwareID(context, device, bufferptr, (uint)(chars * sizeof(char)));
        if (length > 0 && length < (chars * sizeof(char)))
            result = Marshal.PtrToStringAuto(bufferptr);
        Marshal.FreeHGlobal(bufferptr);
        return result;
    }

    /*
    InterceptionContext INTERCEPTION_API interception_create_context(void);
    void INTERCEPTION_API interception_destroy_context(InterceptionContext context);
    InterceptionPrecedence INTERCEPTION_API interception_get_precedence(InterceptionContext context, InterceptionDevice device);
    void INTERCEPTION_API interception_set_precedence(InterceptionContext context, InterceptionDevice device, InterceptionPrecedence precedence);
    InterceptionFilter INTERCEPTION_API interception_get_filter(InterceptionContext context, InterceptionDevice device);
    void INTERCEPTION_API interception_set_filter(InterceptionContext context, InterceptionPredicate predicate, InterceptionFilter filter);
    InterceptionDevice INTERCEPTION_API interception_wait(InterceptionContext context);
    InterceptionDevice INTERCEPTION_API interception_wait_with_timeout(InterceptionContext context, unsigned long milliseconds);
    int INTERCEPTION_API interception_send(InterceptionContext context, InterceptionDevice device, const InterceptionStroke *stroke, unsigned int nstroke);
    int INTERCEPTION_API interception_receive(InterceptionContext context, InterceptionDevice device, InterceptionStroke *stroke, unsigned int nstroke);
    unsigned int INTERCEPTION_API interception_get_hardware_id(InterceptionContext context, InterceptionDevice device, void *hardware_id_buffer, unsigned int buffer_size);
    int INTERCEPTION_API interception_is_invalid(InterceptionDevice device);
    int INTERCEPTION_API interception_is_keyboard(InterceptionDevice device);
    int INTERCEPTION_API interception_is_mouse(InterceptionDevice device);
    */
}
