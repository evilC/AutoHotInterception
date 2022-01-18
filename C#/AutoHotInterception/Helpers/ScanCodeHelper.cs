using System;
using System.Collections.Generic;
using static AutoHotInterception.Helpers.ManagedWrapper;

namespace AutoHotInterception.Helpers
{
    // Order of the strokes received
    public enum Order
    {
        Normal,  // Stroke order is Key press, Key release (No Extended Modifier)
        Wrapped, // Stroke order is Ext Modifier press, Key press, Key release, Ext Modifier Release
        Prefixed // Stroke order is Ext Modifier press, Key press, Ext Modifier release, Key release
    };

    public static class ScanCodeHelper
    {
        // Converts Interception state to AHK state
        private static List<ushort> _stateConverter = new List<ushort>() { 1, 0, 1, 0, 1, 0 };
        // Converts state to extended mode
        private static List<ushort> _stateToExtendedMode = new List<ushort>() { 0, 0, 1, 1, 2, 2 };

        // Keys which have an E0 state, but AHK uses High (+256) code
        private static HashSet<ushort> _highCodeE0Keys = new HashSet<ushort>()
        {
            28, // Numpad Enter
            54, // Right Shift
            69, // Numlock
        };

        // Keys which have an E0 state, but have no extended modifier
        private static HashSet<ushort> _e1Keys = new HashSet<ushort>()
        {
            29, // Right Control
            53, // Numpad Div
            56, // Right Alt
            91, // Left Windows
            92, // Right Windows
            93, // Apps
        };

        // List of two-stroke keys, used to build _twoStrokeKeyConverter
        // Also used by SendKeyEvent to work out what extended keys to send
        public static readonly Dictionary<ushort, Order> _twoStrokeKeys = new Dictionary<ushort, Order>()
        {
            { 55, Order.Wrapped }, // PrtScr
            { 69, Order.Prefixed }, // Pause
            { 71, Order.Wrapped }, // Home
            { 72, Order.Wrapped }, // Up
            { 73, Order.Wrapped }, // PgUp
            { 75, Order.Wrapped }, // Left
            { 77, Order.Wrapped }, // Right
            { 79, Order.Wrapped }, // End
            { 80, Order.Wrapped }, // Down
            { 81, Order.Wrapped }, // PgDn
            { 82, Order.Wrapped }, // Insert
            { 83, Order.Wrapped }, // Delete
        };

        // Lookup table to convert two-stroke keys to code and state
        private static Dictionary<Tuple<ushort, ushort, ushort, ushort>, TranslatedKey>
            _twoStrokeKeyConverter = new Dictionary<Tuple<ushort, ushort, ushort, ushort>, TranslatedKey>();

        static ScanCodeHelper()
        {
            foreach (var item in _twoStrokeKeys)
            {
                var twoStrokeKey = new TwoStrokeKey(item.Key, item.Value);
                _twoStrokeKeyConverter.Add(twoStrokeKey.PressTuple, twoStrokeKey.PressKey);
                _twoStrokeKeyConverter.Add(twoStrokeKey.ReleaseTuple, twoStrokeKey.ReleaseKey);
            }
        }

        /// <summary>
        /// Used by ProcessStrokes() KeyboardHandler to translate incoming key(s) from Interception to AHK format
        /// </summary>
        /// <param name="strokes">A list of one or two Strokes that describe a single key</param>
        /// <returns>An AHK ScanCode and State</returns>
        public static TranslatedKey TranslateScanCodes(List<Stroke> strokes)
        {
            if (strokes.Count == 2)
            {
                return _twoStrokeKeyConverter[
                    new Tuple<ushort, ushort, ushort, ushort>(strokes[0].key.code, strokes[0].key.state, strokes[1].key.code, strokes[1].key.state)];
            }
            else if (strokes.Count == 1)
            {
                var stroke = strokes[0];
                var code = stroke.key.code;
                var state = _stateConverter[stroke.key.state];
                if (_highCodeE0Keys.Contains(stroke.key.code))
                {
                    // Stroke is E0, but AHK code is High (+256)
                    code += 256;
                }
                else
                {
                    if (_stateToExtendedMode[stroke.key.state] > 0)
                    {
                        // Stroke is E1 or E2
                        code += 256;
                    }
                }
                return new TranslatedKey(code, state);
            }
            else
            {
                throw new Exception($"Expected 1 or 2 strokes, but got {strokes.Count}");
            }
        }

        /// <summary>
        /// Used by SendKeyEvent() in KeyboardHandler to translate from AHK code / state into Interception Stroke(s)
        /// </summary>
        /// <param name="code">The AHK code of the key</param>
        /// <param name="state">The AH< state of the key/param>
        /// <returns>A list of Strokes to send to simulate this key being pressed</returns>
        public static List<Stroke> TranslateAhkCode(ushort code, int ahkState)
        {
            var strokes = new List<Stroke>();
            Order order;
            ushort state = (ushort)(1 - ahkState);
            if (code > 256)
            {
                code -= 256;
                if (_highCodeE0Keys.Contains(code) || _e1Keys.Contains(code))
                {
                    order = Order.Normal;
                }
                else if (_twoStrokeKeys.ContainsKey(code))
                {
                    order = _twoStrokeKeys[code];
                }
                else
                {
                    throw new Exception($"Do not know how to handle ScanCode of {code}");
                }
            }
            else if (code == 69)
            {
                order = Order.Prefixed;
            }
            else
            {
                order = Order.Normal;
            }

            if (_e1Keys.Contains(code))
            {
                state += 2;
            }
            else
            {
                state += (ushort)((ushort)order * 2);
            }
            
            if (order == Order.Normal)
            {
                strokes.Add(new Stroke() { key = { code = code, state = state } });
            }
            else if (order == Order.Wrapped)
            {
                // Wrapped (E1)
                if (ahkState == 1)
                {
                    // Press
                    strokes.Add(new Stroke() { key = { code = 42, state = state } });
                    strokes.Add(new Stroke() { key = { code = code, state = state } });
                }
                else
                {
                    // Release
                    strokes.Add(new Stroke() { key = { code = code, state = state } });
                    strokes.Add(new Stroke() { key = { code = 42, state = state } });
                }
            }
            else
            {
                // Prefixed (E2)
                if (ahkState == 1)
                {
                    // Press
                    strokes.Add(new Stroke() { key = { code = 29, state = state } });
                    strokes.Add(new Stroke() { key = { code = code, state = state } });
                }
                else
                {
                    // Release
                    strokes.Add(new Stroke() { key = { code = 29, state = state } });
                    strokes.Add(new Stroke() { key = { code = code, state = state } });
                }
            }

            return strokes;
        }

    }

    // Holds the AHK code and state equivalent of a one or two-stroke set
    public class TranslatedKey
    {
        public ushort Code { get; }
        public int State { get; }

        public TranslatedKey(ushort code, int state)
        {
            Code = code;
            State = state;
        }
    }

    // Builds entries for _twoStrokeKeyConverter
    public class TwoStrokeKey
    {
        public Tuple<ushort, ushort, ushort, ushort> PressTuple { get; }
        public Tuple<ushort, ushort, ushort, ushort> ReleaseTuple { get; }
        public TranslatedKey PressKey { get; }
        public TranslatedKey ReleaseKey { get; }

        public TwoStrokeKey(ushort code, Order order)
        {
            if (order == Order.Prefixed)
            {
                PressTuple = new Tuple<ushort, ushort, ushort, ushort>(29, 4, code, 0);
                ReleaseTuple = new Tuple<ushort, ushort, ushort, ushort>(29, 5, code, 1);
                PressKey = new TranslatedKey(code, 1);
                ReleaseKey = new TranslatedKey(code, 0);
            }
            else if (order == Order.Wrapped)
            {
                PressTuple = new Tuple<ushort, ushort, ushort, ushort>(42, 2, code, 2);
                ReleaseTuple = new Tuple<ushort, ushort, ushort, ushort>(code, 3, 42, 3);
                code += 256;
                PressKey = new TranslatedKey(code, 1);
                ReleaseKey = new TranslatedKey(code, 0);
            }
            else
            {
                throw new Exception("Is not a two-stroke key");
            }
        }
    }
}
