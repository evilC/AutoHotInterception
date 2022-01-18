using System;
using System.Collections.Generic;
using static AutoHotInterception.Helpers.ManagedWrapper;

/*
AHK uses a single ScanCode (0...512) to identify each key, whereas Interception uses two codes - the ScanCode, plus the state.
For example:
in Interception - Code 29 with State 0 or 1 is LCtrl, and 29 with state 2/3 (With state of 2/3 meaning "Extended") is RCtrl
In AHK, code 29 is LCtrl, whereas state 285 (29 + 256) - ie "Extended" versions of keys are Interception code + 256

Furthermore, for some keys (eg Arrow keys and the 6 keys above the arrow key block -  Ins, Del etc), Interception will sometimes receive TWO strokes...
... when one of these keys are pressed - a modifier key (Ctrl or Shift) and the key itself...

Also, AHK assigns "high" (+256) codes to some keys, even when state is 0/1...
... And also assigns a "low" code to Pause, even though it is wrapped in an Extended LCtrl

The purpose of this class is to encapsulate the logic required to deal with this scenario, and assign one code to any key, as AHK does
*/
namespace AutoHotInterception.Helpers
{
    public class TranslatedKey
    {
        public ushort AhkCode { get; set; }
        public List<KeyStroke> Strokes { get; set; }
        //public bool IsExtended { get;  }
        public int State { get; set; }

        public TranslatedKey(KeyStroke stroke, bool isExtended)
        {
            Strokes = new List<KeyStroke>() { stroke };
        }
    }

    public static class SpecialKeys
    {
        public static SpecialKey NumpadEnter { get; set; } = new SpecialKey("Numpad Enter", 28, ExtMode.E0, CodeType.High, Order.Normal);
        public static SpecialKey RightControl { get; } = new SpecialKey("Right Control", 29, ExtMode.E1, CodeType.High, Order.Normal);
        public static SpecialKey NumpadDiv { get; } = new SpecialKey("Numpad Div", 53, ExtMode.E1, CodeType.High, Order.Normal);
        public static SpecialKey RightShift { get; set; } = new SpecialKey("Right Shift", 54, ExtMode.E0, CodeType.High, Order.Normal);
        public static SpecialKey RightAlt { get; } = new SpecialKey("Right Alt", 56, ExtMode.E1, CodeType.High, Order.Normal);
        public static SpecialKey Numlock { get; set; } = new SpecialKey("Numlock", 69, ExtMode.E0, CodeType.High, Order.Normal);
        public static SpecialKey Pause { get; set; } = new SpecialKey("Pause", 69, ExtMode.E0, CodeType.Low, Order.Prefixed);
        public static SpecialKey Home { get; set; } = new SpecialKey("Home", 71, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey Up { get; set; } = new SpecialKey("Up", 72, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey PgUp { get; set; } = new SpecialKey("PgUp", 73, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey Left { get; set; } = new SpecialKey("Left", 75, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey Right { get; set; } = new SpecialKey("Right", 77, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey End { get; set; } = new SpecialKey("End", 79, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey Down { get; set; } = new SpecialKey("Down", 80, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey PgDn { get; set; } = new SpecialKey("PgDn", 81, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey Insert { get; set; } = new SpecialKey("Insert", 82, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey Delete { get; set; } = new SpecialKey("Delete", 83, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static SpecialKey LeftWindows { get; } = new SpecialKey("Left Windows", 91, ExtMode.E1, CodeType.High, Order.Normal);
        public static SpecialKey RightWindows { get; } = new SpecialKey("Right Windows", 92, ExtMode.E1, CodeType.High, Order.Normal);
        public static SpecialKey Apps { get; } = new SpecialKey("Apps", 93, ExtMode.E1, CodeType.High, Order.Normal);

        public static List<SpecialKey> List { get; set; } = new List<SpecialKey>()
        {
            NumpadEnter, RightControl, NumpadDiv, RightShift, RightAlt, Numlock, Pause,
            Home, Up, PgUp, Left, Right, End, Down, PgDn, Insert, Delete,
            LeftWindows, RightWindows, Apps
        };
    }

    // Whether the AHK ScanCode is Low (same as Interception) or Hight (Interception + 256)
    public enum CodeType { Low, High };

    // Whether Press/Release states are 0/1 (E0), 2/3 (E1) or 4/5 (E2)
    public enum ExtMode { E0, E1, E2};
    
    // Order of the strokes received
    public enum Order { Normal /* Stroke order is Key press, Key release (No Extended Modifier) */
            , Wrapped /* Stroke order is Ext Modifier press, Key press, Key release, Ext Modifier Release */
            , Prefixed /* Stroke order is Ext Modifier press, Key press, Ext Modifier release, Key release */};

    public class SpecialKey
    {
        public string Name { get; }
        public ushort Code { get; }
        public ExtMode ExtendedMode { get; }
        public CodeType CodeType { get; }
        public Order StrokeOrder { get; }

        public SpecialKey(string name, ushort code, ExtMode extendedMode, CodeType codeType, Order strokeOrder)
        {
            // The name of the key
            Name = name;
            // The code that identifies this key
            Code = code;
            // What values will be reported for press/release states for this key
            ExtendedMode = extendedMode;
            // Whether AHK uses a High (+256) or Low code for this key
            CodeType = codeType;
            // Whether part of two stroke extended set, and if so, which order the strokes come in
            StrokeOrder = strokeOrder;
        }
    }

    public class ScanCodeHelper
    {
        private TranslatedKey _translatedKey;
        // Converts Interception state to AHK state
        private static List<ushort> _stateConverter = new List<ushort>() { 1, 0, 1, 0, 1, 0 };
        // Converts state to extended mode
        private static List<ushort> _stateToExtendedMode = new List<ushort>() { 0, 0, 1, 1, 2, 2 };
        // List of code/states which signify first stroke of a two stroke set
        private static HashSet<Tuple<ushort, ushort>> _extendedCodeAndStates = new HashSet<Tuple<ushort, ushort>>();

         public ScanCodeHelper()
        {
            for (int i = 0; i < SpecialKeys.List.Count; i++)
            {
                var specialKey = SpecialKeys.List[i];
                var dict = specialKey.CodeType == CodeType.Low ? _lowCodes : _highCodes;
                dict.Add(specialKey.Code, specialKey.Name);
                // Build list of codes which signify that this is the first stroke of an extended set
                if (specialKey.StrokeOrder == Order.Wrapped)
                {
                    _extendedCodeAndStates.Add(new Tuple<ushort, ushort>(42, 2)); // LShift with E1 state on press
                    _extendedCodeAndStates.Add(new Tuple<ushort, ushort>(specialKey.Code, 3)); // ScanCode with E1 state on release
                }
                else if (specialKey.StrokeOrder == Order.Prefixed)
                {
                    _extendedCodeAndStates.Add(new Tuple<ushort, ushort>(29, 4)); // LCtrl with E2 state on press
                    _extendedCodeAndStates.Add(new Tuple<ushort, ushort>(29, 5)); // LCtrl with E2 state on release
                }
            }
        }

        private Dictionary<ushort, string> _highCodes = new Dictionary<ushort, string>();
        private Dictionary<ushort, string> _lowCodes = new Dictionary<ushort, string>();

        public TranslatedKey TranslateScanCode(KeyStroke stroke)
        {
            if (_extendedCodeAndStates.Contains(new Tuple<ushort, ushort>(stroke.code, stroke.state)) || _translatedKey != null)
            {
                // Stroke is first key of Extended key sequence of 2 keys
                if (_translatedKey == null)
                {
                    // Stroke is first of an Extended key sequence
                    // Add this stroke to a buffer, so that we can examine it when the next stroke comes through...
                    // ... and instruct ProcessStroke to take no action for this stroke, as we do not know what the sequence is yet
                    _translatedKey = new TranslatedKey(stroke, true);
                    return null;
                }
                else
                {
                    // Stroke is 2nd of Extended key sequence - we now know what the full sequence is
                    _translatedKey.Strokes.Add(stroke);
                    //var extMode = _stateToExtendedMode[stroke.state];
                    var extMode = _stateToExtendedMode[_translatedKey.Strokes[0].state];
                    ushort state;
                    KeyStroke whichStroke;
                    switch (extMode)
                    {
                        case 0:
                            throw new Exception("Expecting E1 or E2 state");
                        case 1:
                            // E1 (Home, Up, PgUp, Left, Right, End, Down, PgDn, Insert, Delete)
                            // Which state to report (1 = press, 0 = release)
                            state = _stateConverter[_translatedKey.Strokes[0].state];
                            // Which code to use depends on whether this is a press or release
                            // On press, use the second stroke (index 1)
                            // On release, use the first stroke (index 0)
                            whichStroke = _translatedKey.Strokes[state];
                            _translatedKey.AhkCode = (ushort)(whichStroke.code + 256);
                            _translatedKey.State = state;
                            break;
                        case 2:
                            // E2 (Pause only)
                            // Which state to report (1 = press, 0 = release)
                            state = _stateConverter[_translatedKey.Strokes[0].state];
                            // Always use the code of the second stroke (index 1)
                            whichStroke = _translatedKey.Strokes[1];
                            _translatedKey.AhkCode = whichStroke.code;
                            _translatedKey.State = state;
                            break;
                        default:
                            throw new Exception("state can only be E0, E1 or E2");
                    }
                }
            }
            else
            {
                // Stroke is a single key sequence
                _translatedKey = new TranslatedKey(stroke, false);
                var code = stroke.code;
                if (_highCodes.ContainsKey(code))
                {
                    code += 256;
                }
                _translatedKey.AhkCode = code;
                _translatedKey.State = _stateConverter[stroke.state];
            }

            // Code will only get here if the stroke was a single key, or the second key of an extended sequence
            // Return _translatedKey and clear it, ready for the next key
            var returnValue = _translatedKey;
            _translatedKey = null;
            return returnValue;
        }
    }
}
