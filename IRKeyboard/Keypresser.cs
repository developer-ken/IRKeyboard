using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IRKeyboard
{
    class Keypresser
    {
        public static bool UseHardwareInput = false;
        public static Action<byte, byte, byte, byte> SendHWCmd;
        /// <param name="bVk" >按键的虚拟键值</param>
        /// <param name= "bScan" >扫描码，一般不用设置，用0代替就行</param>
        /// <param name= "dwFlags" >选项标志：0：表示按下，2：表示松开</param>
        /// <param name= "dwExtraInfo">一般设置为0</param>
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        public static void KeyPress(byte keycode)
        {
            var ascii = KeyCodeMapAscii.GetAscii(keycode);
            if (UseHardwareInput && ascii != 0)
                SendHWCmd(BaseCommand.KeyPress, ascii, 0, 0);
            else
            {
                KeyDown(keycode);
                KeyUp(keycode);
            }
        }

        public static void KeyDown(byte keycode)
        {
            var ascii = KeyCodeMapAscii.GetAscii(keycode);
            if (UseHardwareInput && ascii != 0)
                SendHWCmd(BaseCommand.KeyDown, ascii, 0, 0);
            else
            {
                keybd_event(keycode, 0, 0, 0);
            }
        }

        public static void KeyUp(byte keycode)
        {
            var ascii = KeyCodeMapAscii.GetAscii(keycode);
            if (UseHardwareInput && ascii != 0)
                SendHWCmd(BaseCommand.KeyUp, ascii, 0, 0);
            else
            {
                keybd_event(keycode, 0, 2, 0);
            }
        }
    }
}
