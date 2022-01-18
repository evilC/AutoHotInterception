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

        // List of two-stroke keys, used to build _twoStrokeKeyConverter
        private static Dictionary<ushort, Order> _twoStrokeKeys = new Dictionary<ushort, Order>()
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
