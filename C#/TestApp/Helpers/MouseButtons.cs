using System.Collections.Generic;

namespace TestApp.Helpers
{
    static class MouseButtons {
        public static List<string> Name { get; } = new List<string>()
        {
            "Left", "Right", "Middle", "XButton 1", "XButton 2", "Scroll (Vertical)", "Scroll (Horizontal)"
        };

        public static MouseButton Left { get; } = new MouseButton("Left", 0);
        public static MouseButton Right { get; } = new MouseButton("Right", 1);
        public static MouseButton Middle { get; } = new MouseButton("Middle", 2);
        public static MouseButton XButton1 { get; } = new MouseButton("XButton 1", 3);
        public static MouseButton XButton2 { get; } = new MouseButton("XButton 2", 4);
        public static MouseButton ScrollVertical { get; } = new MouseButton("Scroll (Vertical)", 5);
        public static MouseButton ScrollHorizontal { get; } = new MouseButton("Scroll (Horizontal)", 6);
    }

    class MouseButton
    {
        public MouseButton(string name, ushort code)
        {
            Name = name;
            Code = code;
        }
        public string Name { get; }
        public ushort Code { get; }
    }
}
