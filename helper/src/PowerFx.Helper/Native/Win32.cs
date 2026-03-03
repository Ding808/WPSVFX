using System.Runtime.InteropServices;

namespace PowerFx.Helper.Native;

/// <summary>
/// 集中声明所有 Win32 P/Invoke，避免分散在各处。
/// </summary>
internal static class Win32
{
    // ── 窗口信息 ─────────────────────────────────────────────────────────────

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    /// <summary>返回指定窗口的每英寸点数（例如 96=100%，144=150%，192=200%）</summary>
    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd); // 最小化

    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// 获取窗口类名
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    // ── 窗口移动 ─────────────────────────────────────────────────────────────

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy,
        uint uFlags);

    // SWP flags
    public const uint SWP_NOSIZE      = 0x0001;
    public const uint SWP_NOMOVE      = 0x0002;
    public const uint SWP_NOZORDER    = 0x0004;
    public const uint SWP_NOACTIVATE  = 0x0010;
    public const uint SWP_SHOWWINDOW  = 0x0040;

    // hWndInsertAfter constants
    public static readonly IntPtr HWND_TOPMOST    = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST  = new(-2);
    public static readonly IntPtr HWND_TOP        = new(0);
    public static readonly IntPtr HWND_BOTTOM     = new(1);

    // ── 钩子 ─────────────────────────────────────────────────────────────────

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    public delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    public const int WH_KEYBOARD_LL = 13;
    public const int WH_MOUSE_LL    = 14;
    public const int HC_ACTION      = 0;

    public const int WM_KEYDOWN     = 0x0100;
    public const int WM_SYSKEYDOWN  = 0x0104;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP   = 0x0202;
    public const int WM_MOUSEMOVE   = 0x0200;

    // 虚拟键码
    public const int VK_BACK   = 0x08;
    public const int VK_DELETE = 0x2E;
    public const int VK_RETURN = 0x0D;
    public const int VK_CONTROL = 0x11;
    public const int VK_LCONTROL = 0xA2;
    public const int VK_RCONTROL = 0xA3;
    public const int VK_KEY_A   = 0x41;

    // GetKeyState 高位表示按下
    [DllImport("user32.dll")]
    public static extern short GetKeyState(int nVirtKey);

    // ── 扩展窗口样式（overlay 用）────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public const int GWL_EXSTYLE      = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_LAYERED    = 0x00080000;
    public const int WS_EX_TOPMOST    = 0x00000008;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    // ── 结构体 ───────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;

        public int Width  => Right  - Left;
        public int Height => Bottom - Top;
    }

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
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;
    }

    // ── 客户区坐标转换 ────────────────────────────────────────────────────────

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    // ── 进程枚举（ToolHelp32）────────────────────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    public const uint TH32CS_SNAPPROCESS = 0x00000002;
    public static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

    // ── Console API ───────────────────────────────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    public const int STD_OUTPUT_HANDLE = -11;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetConsoleScreenBufferInfo(
        IntPtr hConsoleOutput,
        out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCurrentConsoleFontEx(
        IntPtr hConsoleOutput,
        [MarshalAs(UnmanagedType.Bool)] bool bMaximumWindow,
        ref CONSOLE_FONT_INFOEX lpConsoleCurrentFontEx);

    // ── Console 结构体 ────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    public struct COORD
    {
        public short X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SMALL_RECT
    {
        public short Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public short wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CONSOLE_FONT_INFOEX
    {
        public uint cbSize;
        public uint nFont;
        public COORD dwFontSize;
        public int FontFamily;
        public int FontWeight;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FaceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }
}
