using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRKeyboard
{
    internal static class KeyCodeMapAscii
    {
        static public Dictionary<byte, byte> Keycode2Ascii = new Dictionary<byte, byte>()
        {
            {17,128},
            {16,129},
            {18,130},
            {91,131},

            //上下左右
            {38,218},
            {40,217},
            {37,216},
            {39,215},

            {8,178},
            {9,179},
            {13,176},
            {27,177},

            {45,209},
            {46,212},
            {33,211},
            {34,214},

            {36,210},
            {35,213},
            {20,193}
        };

        static public byte GetAscii(byte keycode)
        {
            if (48 <= keycode && keycode <= 57)
            {//0~9
                return keycode;
            }
            if (65 <= keycode && keycode <= 90)
            {//A~Z
                return (byte)(keycode + 32);
            }
            if (112 <= keycode && keycode <= 123)
            {//F1~F12
                return (byte)(keycode - 112 + 194);
            }
            if (124 <= keycode && keycode <= 135)
            {//F13~F24
                return (byte)(keycode - 124 + 240);
            }
            if (Keycode2Ascii.ContainsKey(keycode))
            {
                return Keycode2Ascii[keycode]; ;
            }
            return 0;
        }
    }
}
