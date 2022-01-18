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
        //public List<KeyStroke> Strokes { get; set; }
        public KeyStroke FirstStroke { get; }
        public KeyStroke SecondStroke { get; set; }
        public bool IsExtended { get;  }
        public int State { get; set; }

        public TranslatedKey(KeyStroke stroke, bool isExtended)
        {
            //Strokes = new List<KeyStroke>() { stroke };
            FirstStroke = stroke;
        }
    }

    public static class SpecialKeys
    {
        public static SpecialKey NumpadEnter { get; set; } = new SpecialKey("Numpad Enter", 28, ExtMode.E0, CodeType.High, Order.Normal);
        public static SpecialKey RightControl { get; } = new SpecialKey("Right Control", 29, ExtMode.E1, CodeType.High, Order.Normal);
        public static SpecialKey NumpadDiv { get; } = new SpecialKey("Numpad Div", 53, ExtMode.E1, CodeType.High, Order.Normal);
        public static SpecialKey RightShift { get; set; } = new SpecialKey("Right Shift", 54, ExtMode.E0, CodeType.High, Order.Normal);
        public static SpecialKey Pause { get; set; } = new SpecialKey("Pause", 69, ExtMode.E0, CodeType.Low, Order.Prefixed);
        public static SpecialKey Home { get; set; } = new SpecialKey("Home", 71, ExtMode.E1, CodeType.High, Order.Wrapped);
        public static List<SpecialKey> List { get; set; } = new List<SpecialKey>()
        {
            NumpadEnter, RightControl, NumpadDiv, Pause, Home
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

        public SpecialKey(string name, ushort code, ExtMode extendedMode, CodeType codeType, Order strokeOrderW)
        {
            // The name of the key
            Name = name;
            // The code that identifies this key
            Code = code;
            // What values will be reported for press/release states for this key
            ExtendedMode = extendedMode;
            // Whether AHK uses a High (+256) or Low code for this key
            CodeType = codeType;
        }
    }

    public class ScanCodeHelper
    {
        //private KeyStroke? _extendedBuffer;
        private TranslatedKey _translatedKey;
        // Converts Interception state to AHK state
        private static List<ushort> _stateConverter = new List<ushort>() { 1, 0 , 1, 0, 1, 0 };
        // Converts state to extended mode
        private static List<ushort> _stateToExtendedMode = new List<ushort>() { 0, 0, 1, 1, 2, 2 };

        /*
        // Keys which AHK assigns a high code to, even though state is 0/1
        // These keys also do not generate extended key codes
        private Dictionary<ushort, string> _highCodes = new Dictionary<ushort, string>()
        {
            { 28, "Nummpad Enter" },
            { 54, "Right Shift" },
            { 69, "Numlock" },

        };


        // Keys which have an extended state, but extended modifiers are not sent
        private Dictionary<ushort, string> _noExtendedModifier = new Dictionary<ushort, string>()
        {
            {29, "Right Control" },
            {53, "Numpad Div" },
            {56, "Right Alt" },
            {91, "Left Windows" },
            {92, "Right Windows" },
            {93, "Apps" }
        };
        */

        public ScanCodeHelper()
        {
            for (int i = 0; i < SpecialKeys.List.Count; i++)
            {
                var specialKey = SpecialKeys.List[i];
                var dict = specialKey.CodeType == CodeType.Low ? _lowCodes : _highCodes;
                dict.Add(specialKey.Code, specialKey.Name);
                if (specialKey.ExtendedMode != ExtMode.E0 && specialKey.StrokeOrder == Order.Normal)
                {
                    _noExtendedModifier.Add(specialKey.Code, specialKey.Name);
                }
            }
        }

        private Dictionary<ushort, string> _highCodes = new Dictionary<ushort, string>();
        private Dictionary<ushort, string> _lowCodes = new Dictionary<ushort, string>();
        // Keys which have E1 or E2 state, but do not send extended modifier
        private Dictionary<ushort, string> _noExtendedModifier = new Dictionary<ushort, string>();

        public TranslatedKey TranslateScanCode(KeyStroke stroke)
        {
            //if(stroke.state > 1 && !_noExtendedModifier.ContainsKey(stroke.code))
            //if (stroke.state > 1 || stroke.code == SpecialKeys.Pause.Code )
            if (stroke.state > 1 && !_noExtendedModifier.ContainsKey(stroke.code))
            {
                // Stroke is part of Extended key sequence of 2 keys
                if (_translatedKey == null)
                {
                    // Stroke is first of an Extended key sequence
                    // Add this stroke to a buffer, so that we can examine it when the next stroke comes through...
                    // ... and instruct ProcessStroke to take no action for this stroke, as we do not know what the sequence is yet
                    _translatedKey = new TranslatedKey(stroke, true);
                    return null;
                } else
                {
                    // Stroke is 2nd of Extended key sequence - we now know what the full sequence is
                    _translatedKey.SecondStroke = stroke;
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
            var returnValue = _translatedKey;
            if (!_translatedKey.IsExtended)
            {
                _translatedKey = null;
            }
            return returnValue;
        }
    }
}
