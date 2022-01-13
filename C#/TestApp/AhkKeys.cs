using System.Collections.Generic;
/* Get AHK specific Scancodes for a given key, as reported by AHK's GetKeySC() */

namespace TestApp
{
    public class AhkKey
    {
        public string Name { get; }
        public ushort Code { get; }

        public AhkKey(ushort code, string name)
        {
            Name = name;
            Code = code;
        }
    }

    public static class AhkKeys
    {
        private static Dictionary<string, AhkKey> _keyNames = new Dictionary<string, AhkKey>();
        private static Dictionary<ushort, AhkKey> _keyCodes = new Dictionary<ushort, AhkKey>();

        static AhkKeys()
        {
            foreach (var key in KeyList){
                AddKey(key.Key, key.Value);
            }
        }

        // Given a Name, get Code
        public static string Name(ushort code)
        {
            return _keyCodes[code].Name;
        }

        // Given a Code, get Name
        public static int Code(string name)
        {
            return _keyNames[name].Code;
        }

        // Given a Name, get AhkKey object
        public static AhkKey Obj(string name)
        {
            return _keyNames[name];
        }

        // Given a Code, get AhkKey object
        public static AhkKey Obj(ushort code)
        {
            return _keyCodes[code];
        }

        private static void AddKey(ushort code, string name)
        {
            var key = new AhkKey(code, name);
            _keyCodes.Add(code, key);
            _keyNames.Add(name, key);
        }

        public static Dictionary<ushort, string> KeyList = new Dictionary<ushort, string>()
        {
            {1 /*(0x1)*/, "Escape"},
            {2 /*(0x2)*/, "1"},
            {3 /*(0x3)*/, "2"},
            {4 /*(0x4)*/, "3"},
            {5 /*(0x5)*/, "4"},
            {6 /*(0x6)*/, "5"},
            {7 /*(0x7)*/, "6"},
            {8 /*(0x8)*/, "7"},
            {9 /*(0x9)*/, "8"},
            {10 /*(0xa)*/, "9"},
            {11 /*(0xb)*/, "0"},
            {12 /*(0xc)*/, "-"},
            {13 /*(0xd)*/, "="},
            {14 /*(0xe)*/, "Backspace"},
            {15 /*(0xf)*/, "Tab"},
            {16 /*(0x10)*/, "q"},
            {17 /*(0x11)*/, "w"},
            {18 /*(0x12)*/, "e"},
            {19 /*(0x13)*/, "r"},
            {20 /*(0x14)*/, "t"},
            {21 /*(0x15)*/, "y"},
            {22 /*(0x16)*/, "u"},
            {23 /*(0x17)*/, "i"},
            {24 /*(0x18)*/, "o"},
            {25 /*(0x19)*/, "p"},
            {26 /*(0x1a)*/, "["},
            {27 /*(0x1b)*/, "]"},
            {28 /*(0x1c)*/, "Enter"},
            {29 /*(0x1d)*/, "LControl"},
            {30 /*(0x1e)*/, "a"},
            {31 /*(0x1f)*/, "s"},
            {32 /*(0x20)*/, "d"},
            {33 /*(0x21)*/, "f"},
            {34 /*(0x22)*/, "g"},
            {35 /*(0x23)*/, "h"},
            {36 /*(0x24)*/, "j"},
            {37 /*(0x25)*/, "k"},
            {38 /*(0x26)*/, "l"},
            {39 /*(0x27)*/, ";"},
            {40 /*(0x28)*/, "'"},
            {41 /*(0x29)*/, "`"},
            {42 /*(0x2a)*/, "LShift"},
            {43 /*(0x2b)*/, "#"},
            {44 /*(0x2c)*/, "z"},
            {45 /*(0x2d)*/, "x"},
            {46 /*(0x2e)*/, "c"},
            {47 /*(0x2f)*/, "v"},
            {48 /*(0x30)*/, "b"},
            {49 /*(0x31)*/, "n"},
            {50 /*(0x32)*/, "m"},
            {51 /*(0x33)*/, ","},
            {52 /*(0x34)*/, "."},
            {53 /*(0x35)*/, "/"},
            {54 /*(0x36)*/, "Shift"},
            {55 /*(0x37)*/, "NumpadMult"},
            {56 /*(0x38)*/, "LAlt"},
            {57 /*(0x39)*/, "Space"},
            {58 /*(0x3a)*/, "CapsLock"},
            {59 /*(0x3b)*/, "F1"},
            {60 /*(0x3c)*/, "F2"},
            {61 /*(0x3d)*/, "F3"},
            {62 /*(0x3e)*/, "F4"},
            {63 /*(0x3f)*/, "F5"},
            {64 /*(0x40)*/, "F6"},
            {65 /*(0x41)*/, "F7"},
            {66 /*(0x42)*/, "F8"},
            {67 /*(0x43)*/, "F9"},
            {68 /*(0x44)*/, "F10"},
            {69 /*(0x45)*/, "Pause"},
            {70 /*(0x46)*/, "ScrollLock"},
            {71 /*(0x47)*/, "NumpadHome"},
            {72 /*(0x48)*/, "NumpadUp"},
            {73 /*(0x49)*/, "NumpadPgUp"},
            {74 /*(0x4a)*/, "NumpadSub"},
            {75 /*(0x4b)*/, "NumpadLeft"},
            {76 /*(0x4c)*/, "NumpadClear"},
            {77 /*(0x4d)*/, "NumpadRight"},
            {78 /*(0x4e)*/, "NumpadAdd"},
            {79 /*(0x4f)*/, "NumpadEnd"},
            {80 /*(0x50)*/, "NumpadDown"},
            {81 /*(0x51)*/, "NumpadPgDn"},
            {82 /*(0x52)*/, "NumpadIns"},
            {83 /*(0x53)*/, "NumpadDel"},
            {84 /*(0x54)*/, "PrintScreen"},
            {86 /*(0x56)*/, "\\"},
            {87 /*(0x57)*/, "F11"},
            {88 /*(0x58)*/, "F12"},
            {99 /*(0x63)*/, "Help"},
            {100 /*(0x64)*/, "F13"},
            {101 /*(0x65)*/, "F14"},
            {102 /*(0x66)*/, "F15"},
            {103 /*(0x67)*/, "F16"},
            {104 /*(0x68)*/, "F17"},
            {105 /*(0x69)*/, "F18"},
            {106 /*(0x6a)*/, "F19"},
            {107 /*(0x6b)*/, "F20"},
            {108 /*(0x6c)*/, "F21"},
            {109 /*(0x6d)*/, "F22"},
            {110 /*(0x6e)*/, "F23"},
            {118 /*(0x76)*/, "F24"},
            {272 /*(0x110)*/, "Media_Prev"},
            {281 /*(0x119)*/, "Media_Next"},
            {284 /*(0x11c)*/, "NumpadEnter"},
            {285 /*(0x11d)*/, "RControl"},
            {288 /*(0x120)*/, "Volume_Mute"},
            {289 /*(0x121)*/, "Launch_App2"},
            {290 /*(0x122)*/, "Media_Play_Pause"},
            {292 /*(0x124)*/, "Media_Stop"},
            {302 /*(0x12e)*/, "Volume_Down"},
            {304 /*(0x130)*/, "Volume_Up"},
            {306 /*(0x132)*/, "Browser_Home"},
            {309 /*(0x135)*/, "NumpadDiv"},
            {310 /*(0x136)*/, "RShift"},
            {312 /*(0x138)*/, "RAlt"},
            {325 /*(0x145)*/, "Numlock"},
            {326 /*(0x146)*/, "CtrlBreak"},
            {327 /*(0x147)*/, "Home"},
            {328 /*(0x148)*/, "Up"},
            {329 /*(0x149)*/, "PgUp"},
            {331 /*(0x14b)*/, "Left"},
            {333 /*(0x14d)*/, "Right"},
            {335 /*(0x14f)*/, "End"},
            {336 /*(0x150)*/, "Down"},
            {337 /*(0x151)*/, "PgDn"},
            {338 /*(0x152)*/, "Insert"},
            {339 /*(0x153)*/, "Delete"},
            {347 /*(0x15b)*/, "LWin"},
            {348 /*(0x15c)*/, "RWin"},
            {349 /*(0x15d)*/, "AppsKey"},
            {351 /*(0x15f)*/, "Sleep"},
            {357 /*(0x165)*/, "Browser_Search"},
            {358 /*(0x166)*/, "Browser_Favorites"},
            {359 /*(0x167)*/, "Browser_Refresh"},
            {360 /*(0x168)*/, "Browser_Stop"},
            {361 /*(0x169)*/, "Browser_Forward"},
            {362 /*(0x16a)*/, "Browser_Back"},
            {363 /*(0x16b)*/, "Launch_App1"},
            {364 /*(0x16c)*/, "Launch_Mail"},
            {365 /*(0x16d)*/, "Launch_Media"},

            // ============ DUPES ==================
            // {89 /*(0x59)*/, "NumpadClear"} (Also 76)
            // {124 /*(0x7c)*/, "Tab"} (Also 15)
            // {257 /*(0x101)*/, "Escape"} (Also 1)
            // {258 /*(0x102)*/, "1"} (Also 2)
            // {259 /*(0x103)*/, "2"} (Also 3)
            // {260 /*(0x104)*/, "3"} (Also 4)
            // {261 /*(0x105)*/, "4"} (Also 5)
            // {262 /*(0x106)*/, "5"} (Also 6)
            // {263 /*(0x107)*/, "6"} (Also 7)
            // {264 /*(0x108)*/, "7"} (Also 8)
            // {265 /*(0x109)*/, "8"} (Also 9)
            // {266 /*(0x10a)*/, "9"} (Also 10)
            // {267 /*(0x10b)*/, "0"} (Also 11)
            // {268 /*(0x10c)*/, "-"} (Also 12)
            // {269 /*(0x10d)*/, "="} (Also 13)
            // {270 /*(0x10e)*/, "Backspace"} (Also 14)
            // {271 /*(0x10f)*/, "Tab"} (Also 15)
            // {273 /*(0x111)*/, "w"} (Also 17)
            // {274 /*(0x112)*/, "e"} (Also 18)
            // {275 /*(0x113)*/, "r"} (Also 19)
            // {276 /*(0x114)*/, "t"} (Also 20)
            // {277 /*(0x115)*/, "y"} (Also 21)
            // {278 /*(0x116)*/, "u"} (Also 22)
            // {279 /*(0x117)*/, "i"} (Also 23)
            // {280 /*(0x118)*/, "o"} (Also 24)
            // {282 /*(0x11a)*/, "["} (Also 26)
            // {283 /*(0x11b)*/, "]"} (Also 27)
            // {286 /*(0x11e)*/, "a"} (Also 30)
            // {287 /*(0x11f)*/, "s"} (Also 31)
            // {291 /*(0x123)*/, "h"} (Also 35)
            // {293 /*(0x125)*/, "k"} (Also 37)
            // {294 /*(0x126)*/, "l"} (Also 38)
            // {295 /*(0x127)*/, ";"} (Also 39)
            // {296 /*(0x128)*/, "'"} (Also 40)
            // {297 /*(0x129)*/, "`"} (Also 41)
            // {298 /*(0x12a)*/, "Shift"} (Also 54)
            // {299 /*(0x12b)*/, "#"} (Also 43)
            // {300 /*(0x12c)*/, "z"} (Also 44)
            // {301 /*(0x12d)*/, "x"} (Also 45)
            // {303 /*(0x12f)*/, "v"} (Also 47)
            // {305 /*(0x131)*/, "n"} (Also 49)
            // {307 /*(0x133)*/, ","} (Also 51)
            // {308 /*(0x134)*/, "."} (Also 52)
            // {311 /*(0x137)*/, "PrintScreen"} (Also 84)
            // {313 /*(0x139)*/, "Space"} (Also 57)
            // {314 /*(0x13a)*/, "CapsLock"} (Also 58)
            // {315 /*(0x13b)*/, "F1"} (Also 59)
            // {316 /*(0x13c)*/, "F2"} (Also 60)
            // {317 /*(0x13d)*/, "F3"} (Also 61)
            // {318 /*(0x13e)*/, "F4"} (Also 62)
            // {319 /*(0x13f)*/, "F5"} (Also 63)
            // {320 /*(0x140)*/, "F6"} (Also 64)
            // {321 /*(0x141)*/, "F7"} (Also 65)
            // {322 /*(0x142)*/, "F8"} (Also 66)
            // {323 /*(0x143)*/, "F9"} (Also 67)
            // {324 /*(0x144)*/, "F10"} (Also 68)
            // {330 /*(0x14a)*/, "NumpadSub"} (Also 74)
            // {332 /*(0x14c)*/, "NumpadClear"} (Also 76)
            // {334 /*(0x14e)*/, "NumpadAdd"} (Also 78)
            // {340 /*(0x154)*/, "PrintScreen"} (Also 84)
            // {342 /*(0x156)*/, "\"} (Also 86)
            // {343 /*(0x157)*/, "F11"} (Also 87)
            // {344 /*(0x158)*/, "F12"} (Also 88)
            // {345 /*(0x159)*/, "NumpadClear"} (Also 76)
            // {355 /*(0x163)*/, "Help"} (Also 99)
            // {356 /*(0x164)*/, "F13"} (Also 100)
            // {366 /*(0x16e)*/, "F23"} (Also 110)
            // {374 /*(0x176)*/, "F24"} (Also 118)
            // {380 /*(0x17c)*/, "Tab"} (Also 15)

        };
    }
}

// AHK Script to get names
//clipboard := ""
//log := "`n// ============ DUPES ==================`n"
//keys := {}

//Loop 512 {
//	hex := Format("{:x}", A_Index)
//	name := GetKeyName("sc" hex)
//	if (name == "")
//		continue
//	str := "{" A_Index " /*(0x" hex ")*/, " """" name """" "}"
//	;~ if (A_Index == 86 || A_Index = 342)
//		;~ break = true
//	if (keys.HasKey(name)){
//		log.= "// " str " (Also " keys[name] ")`n"
//	} else {
//		clipboard.= str ",`n"
//		keys[name] := A_Index
//	}
//}
//clipboard.= log

