using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AutoHotInterception.Helpers
{
    public static class ManagedWrapper
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Predicate(int device);

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

            MouseButtonAnyDown = MouseState.Button1Down | MouseState.Button2Down | MouseState.Button3Down |
                                 MouseState.Button4Down | MouseState.Button5Down,

            MouseButtonAnyUp = MouseState.Button1Up | MouseState.Button2Up | MouseState.Button3Up |
                               MouseState.Button4Up | MouseState.Button5Up,

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

        static ManagedWrapper()
        {
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var subfolder = Environment.Is64BitProcess ? "\\x64\\" : "\\x86\\";

            LoadLibrary(folder + subfolder + "interception.dll");
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("interception.dll", EntryPoint = "interception_create_context",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateContext();

        [DllImport("interception.dll", EntryPoint = "interception_destroy_context",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyContext(IntPtr context);

        [DllImport("interception.dll", EntryPoint = "interception_set_filter",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetFilter(IntPtr context, Predicate predicate, Filter filter);
        // public static extern void SetFilter(IntPtr context, Predicate predicate, ushort filter);

        // InterceptionFilter INTERCEPTION_API interception_get_filter(InterceptionContext context, InterceptionDevice device);
        [DllImport("interception.dll", EntryPoint = "interception_get_filter",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort GetFilter(IntPtr context, int device);

        [DllImport("interception.dll", EntryPoint = "interception_receive",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int Receive(IntPtr context, int device, ref Stroke stroke, uint nstroke);

        [DllImport("interception.dll", EntryPoint = "interception_send", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Send(IntPtr context, int device, ref Stroke stroke, uint nstroke);

        [DllImport("interception.dll", EntryPoint = "interception_is_keyboard",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsKeyboard(int device);

        [DllImport("interception.dll", EntryPoint = "interception_is_mouse",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsMouse(int device);

        [DllImport("interception.dll", EntryPoint = "interception_is_invalid",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsInvalid(int device);

        //InterceptionDevice INTERCEPTION_API interception_wait(InterceptionContext context);
        [DllImport("interception.dll", EntryPoint = "interception_wait", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Wait(IntPtr context);

        //InterceptionDevice INTERCEPTION_API interception_wait_with_timeout(InterceptionContext context, unsigned long milliseconds);
        [DllImport("interception.dll", EntryPoint = "interception_wait_with_timeout",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int WaitWithTimeout(int device, ulong milliseconds);

        // unsigned int INTERCEPTION_API interception_get_hardware_id(InterceptionContext context, InterceptionDevice device, void *hardware_id_buffer, unsigned int buffer_size);
        [DllImport("interception.dll", EntryPoint = "interception_get_hardware_id",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetHardwareID(IntPtr context, int device, IntPtr hardwareidbuffer, uint buffersize);
        // public static extern uint GetHardwareID(IntPtr context, int device, [MarshalAs(UnmanagedType.ByValArray,SizeConst=500)]char[] hardwareidbuffer, uint buffersize);
        //public static extern uint GetHardwareID(IntPtr context, int device, ref _wchar_t[] hardwareidbuffer, uint buffersize);

        public static string GetHardwareStr(IntPtr context, int device, int chars = 0)
        {
            if (chars == 0)
                chars = 500;
            var result = "";
            var bufferptr = Marshal.StringToHGlobalUni(new string(new char[chars]));
            var length = GetHardwareID(context, device, bufferptr, (uint) (chars * sizeof(char)));
            if (length > 0 && length < chars * sizeof(char))
                result = Marshal.PtrToStringAuto(bufferptr);
            Marshal.FreeHGlobal(bufferptr);
            return result;
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
            [FieldOffset(0)] public MouseStroke mouse;
            [FieldOffset(0)] public KeyStroke key;
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
}