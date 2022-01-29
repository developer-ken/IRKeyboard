using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRKeyboard
{
    class BaseCommand
    {
        public const byte
            KeyDown = 0xBF,
            KeyUp = 0xC0,
            KeyPress = 0xC1,
            MouseMove = 0xC2,
            MouseDown = 0xC3,
            MouseUp = 0xC4,
            LightLevel = 0xC5,
            LightBreath = 0xC6;
    }
}
