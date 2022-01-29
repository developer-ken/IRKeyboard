using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRKeyboard
{
    class BaseCommands
    {
        public const byte
            KeyDown = 0xBF,
            KeyUp = 0xC0,
            KeyPress = 0xC1,
            MouseMove = 0xC2,
            MouseDown = 0xC3,
            MouseUp = 0xC4,
            LightLevel = 0xC5,
            LightBreath = 0xC6,
            HyperSwitch = 0xC7,
            MouseClick = 0xC8,
            KeySwitch = 0xC9,
            MouseSwitch = 0xCA,
            ProgramEEPROM = 0xDD,
            JumpTo = 0xA0,
            DisableEEPROM = 0xEE,
            EnableEEPROM = 0xEF,
            ProbeEEPROM = 0xF0,
            HighPulsLow = 0xCB,
            LowPulsHigh = 0xCC;

        public const byte
            MouseLeft = 1,
            MouseRight = 2,
            MouseMiddle = 4;
    }
}
