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
    class TranslatedKey
    {
        public ushort AhkCode { get; set; }
        //public List<KeyStroke> Strokes { get; set; }
        public KeyStroke FirstStroke { get; }
        public KeyStroke SecondStroke { get; set; }
        public bool IsExtended { get;  }

        public TranslatedKey(KeyStroke stroke, bool isExtended)
        {
            //Strokes = new List<KeyStroke>() { stroke };
            FirstStroke = stroke;
        }
    }


    class ScanCodeHelper
    {
        //private KeyStroke? _extendedBuffer;
        private TranslatedKey _translatedKey;

        // Keys which AHK assigns a 
        private Dictionary<ushort, string> _highCodes = new Dictionary<ushort, string>()
        {
            { 28, "Nummpad Enter" },
            { 54, "Right Shift" },
            { 69, "Numlock" },

        };

        public TranslatedKey TranslateScanCode(KeyStroke stroke)
        {
            if(stroke.state > 1)
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
                // Regular key
                _translatedKey = new TranslatedKey(stroke, false);
                var code = stroke.code;
                if (_highCodes.ContainsKey(code))
                {
                    code += 256;
                }
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
