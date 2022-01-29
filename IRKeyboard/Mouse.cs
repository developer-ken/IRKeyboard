using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRKeyboard
{
    class Mouse
    {
        public static bool UseHardwareInput = false;
        public static Action<byte, byte, byte, byte> SendHWCmd;

        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        //移动鼠标 
        const int MOUSEEVENTF_MOVE = 0x0001;
        //模拟鼠标左键按下 
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        //模拟鼠标左键抬起 
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        //模拟鼠标右键按下 
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        //模拟鼠标右键抬起 
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //模拟鼠标中键按下 
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        //模拟鼠标中键抬起 
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        //标示是否采用绝对坐标 
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        //模拟鼠标滚轮滚动操作，必须配合dwData参数
        const int MOUSEEVENTF_WHEEL = 0x0800;

        public static void MouseMove(int dx, int dy)
        {
            if (UseHardwareInput)
            {
                SendHWCmd(BaseCommands.MouseMove, (byte)(127 + dx), (byte)(127 + dy), 127);
            }
            else
                mouse_event(MOUSEEVENTF_MOVE, dx, dy, 0, 0);
        }

        public static void MouseLDown()
        {
            if (UseHardwareInput)
            {
                SendHWCmd(BaseCommands.MouseDown, 1, 0, 0);
            }
            else
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        }

        public static void MouseLUp()
        {
            if (UseHardwareInput)
            {
                SendHWCmd(BaseCommands.MouseUp, 1, 0, 0);
            }
            else
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public static void MouseRDown()
        {
            if (UseHardwareInput)
            {
                SendHWCmd(BaseCommands.MouseDown, 2, 0, 0);
            }
            else
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        }

        public static void MouseRUp()
        {
            if (UseHardwareInput)
            {
                SendHWCmd(BaseCommands.MouseUp, 2, 0, 0);
            }
            else
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public static void MouseMDown()
        {
            if (UseHardwareInput)
            {
                SendHWCmd(BaseCommands.MouseDown, 4, 0, 0);
            }
            else
                mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
        }

        public static void MouseMUp()
        {
            if (UseHardwareInput)
            {
                SendHWCmd(BaseCommands.MouseUp, 4, 0, 0);
            }
            else
                mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// 滚轮
        /// </summary>
        /// <param name="dtstance">向上为正</param>
        public static void MouseWheel(int dtstance)
        {
            if (UseHardwareInput)
            {
                SendHWCmd(BaseCommands.MouseMove, 0, 0, (byte)(dtstance + 127));
            }
            else
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, dtstance, 0);
        }
    }
}
