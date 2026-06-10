// NativeMethods.cs — all P/Invoke declarations
using System.Runtime.InteropServices;

internal static class NativeMethods
{
    // ── DWM ──────────────────────────────────────────────────────────────────
    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    internal const int DWMWA_USE_IMMERSIVE_DARK_MODE    = 20;
    internal const int DWMWA_WINDOW_CORNER_PREFERENCE   = 33;
    internal const int DWMWCP_ROUND                     = 2;

    // ── Borderless window drag ────────────────────────────────────────────────
    [DllImport("user32.dll")] internal static extern bool  ReleaseCapture();
    [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    internal const uint WM_NCLBUTTONDOWN = 0xA1;
    internal const int  HT_CAPTION       = 2;

    // ── Keyboard hook ────────────────────────────────────────────────────────
    internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] internal static extern bool  UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string? lpModuleName);

    internal const int  WH_KEYBOARD_LL = 13;
    internal const int  WM_KEYDOWN     = 0x0100;
    internal const int  WM_KEYUP       = 0x0101;
    internal const uint VK_NUMLOCK     = 0x90;
    internal const uint LLKHF_INJECTED = 0x10;

    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hWnd);

    // ── Inject keystrokes ─────────────────────────────────────────────────────
    [DllImport("user32.dll")] internal static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint extra);
    internal const uint KEYEVENTF_KEYUP = 2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        public uint  vkCode;
        public uint  scanCode;
        public uint  flags;
        public uint  time;
        public nuint dwExtraInfo;
    }
}
