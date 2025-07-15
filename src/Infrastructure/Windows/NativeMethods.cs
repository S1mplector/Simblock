using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// P/Invoke declarations for Windows API
    /// </summary>
    internal static class NativeMethods
    {
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        public const int HC_ACTION = 0;
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        
        // Mouse messages
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_MOUSEHWHEEL = 0x020E;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_XBUTTONUP = 0x020C;
        
        // Additional virtual key codes
        public const int VK_U = 0x55;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        // Virtual key codes for modifier keys
        public const int VK_CONTROL = 0x11;
        public const int VK_LCONTROL = 0xA2;
        public const int VK_RCONTROL = 0xA3;
        public const int VK_MENU = 0x12;      // Alt key
        public const int VK_LMENU = 0xA4;     // Left Alt
        public const int VK_RMENU = 0xA5;     // Right Alt
        public const int VK_SHIFT = 0x10;     // Shift key
        public const int VK_LSHIFT = 0xA0;    // Left Shift
        public const int VK_RSHIFT = 0xA1;    // Right Shift

        // Keyboard layout related constants
        public const int KLF_ACTIVATE = 0x00000001;
        public const int LOCALE_SLANGUAGE = 0x00000002;
        public const int LOCALE_SENGLANGUAGE = 0x00001001;

        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        public static extern int GetKeyboardLayoutName(System.Text.StringBuilder pwszKLID);

        [DllImport("kernel32.dll")]
        public static extern int GetLocaleInfo(uint Locale, uint LCType, System.Text.StringBuilder lpLCData, int cchData);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        // Mouse information API calls
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        // System metrics constants for mouse information
        public const int SM_CMOUSEBUTTONS = 43;     // Number of mouse buttons
        public const int SM_MOUSEPRESENT = 19;      // Mouse present
        public const int SM_MOUSEWHEELPRESENT = 75; // Mouse wheel present
        public const int SM_MOUSEHORIZONTALWHEELPRESENT = 91; // Horizontal mouse wheel present
        public const int SM_SWAPBUTTON = 23;        // Mouse buttons swapped

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public int x;
            public int y;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
