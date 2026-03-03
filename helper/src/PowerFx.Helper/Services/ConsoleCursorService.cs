using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using PowerFx.Helper.Native;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 通过 Win32 Console API 获取 Windows Terminal 当前光标的屏幕坐标。
///
/// 原理（无需 DLL 注入，全部公开 API）：
///   1. 从 WT 进程树中找到正在运行的 shell 子进程（pwsh/cmd/bash 等）
///   2. AttachConsole(shellPid) → GetConsoleScreenBufferInfo 得到光标行列号
///   3. GetCurrentConsoleFontEx 得到字符单元格像素尺寸
///   4. GetClientRect + ClientToScreen 得到 WT 窗口客户区原点
///   5. 加上 Tab 栏高度 + Padding 偏移 → 换算为屏幕坐标
///
/// 近似说明：
///   Tab 栏高度和 Padding 使用 WT 默认值估算，
///   不同主题/设置下可能有数像素偏差，但远优于随机位置。
/// </summary>
public sealed class ConsoleCursorService : IDisposable
{
    // WT 默认布局常量（像素）
    private const int DefaultTabBarHeight  = 36;
    private const int DefaultContentPadding = 8;

    // 已知 shell 进程名（不含 .exe）
    private static readonly HashSet<string> KnownShells = new(StringComparer.OrdinalIgnoreCase)
    {
        "powershell", "pwsh", "cmd", "bash", "wsl", "wslhost",
        "nu", "fish", "zsh", "sh", "elvish", "nushell",
        "python", "python3", "node"
    };

    // AttachConsole 全局互斥（同一进程同时只能 attach 一个 console）
    private static readonly object _consoleLock = new();

    private bool _disposed;

    /// <summary>
    /// 查询光标屏幕坐标。
    /// hwnd 为当前 WT 窗口句柄；tabBarHeight、contentPadding 可从 settings.json 读取，
    /// 传 -1 则使用默认值。
    /// 失败时返回 null（调用方降级为随机位置）。
    /// </summary>
    public Point? GetCursorScreenPoint(
        IntPtr wtHwnd,
        int tabBarHeight    = -1,
        int contentPadding  = -1)
    {
        if (wtHwnd == IntPtr.Zero) return null;

        // 获取 WT 窗口真实 DPI，将逻辑像素常量换算为物理像素
        uint dpi = Win32.GetDpiForWindow(wtHwnd);
        if (dpi == 0) dpi = 96;
        double dpiScale = dpi / 96.0;

        tabBarHeight   = tabBarHeight   < 0 ? DefaultTabBarHeight   : tabBarHeight;
        contentPadding = contentPadding < 0 ? DefaultContentPadding : contentPadding;

        // 逻辑像素 → 物理像素
        int tabBarPx  = (int)Math.Round(tabBarHeight   * dpiScale);
        int paddingPx = (int)Math.Round(contentPadding * dpiScale);

        // 1. WT 进程 PID
        Win32.GetWindowThreadProcessId(wtHwnd, out uint wtPid);
        if (wtPid == 0) return null;

        // 2. 找 shell 子进程（深度 1-2）
        uint shellPid = FindShellChildPid(wtPid);
        if (shellPid == 0)
        {
            Logger.Warn("ConsoleCursor", $"WT PID={wtPid} 未找到已知 shell 子进程，降级为 null");
            return null;
        }
        Logger.Debug("ConsoleCursor", $"找到 shell 子进程 PID={shellPid}");

        // 3. 通过 Console API 获取光标行列 + 字符单元格大小 + 视口总行数
        if (!TryGetConsoleCursorInfo(shellPid,
                out short col, out short visibleRow,
                out short cellW, out short cellH,
                out short viewportRows))
            return null;

        // 4. WT 窗口客户区矩形 + 客户区原点（屏幕坐标）
        if (!Win32.GetClientRect(wtHwnd, out _)) return null;
        var clientOrigin = new Win32.POINT { X = 0, Y = 0 };
        if (!Win32.ClientToScreen(wtHwnd, ref clientOrigin)) return null;

        // 5. 换算（全部物理像素）
        // ConPTY 视口从内容顶部往下渲染，visRow=0 = 第一行
        int screenX = clientOrigin.X + paddingPx + col      * cellW;
        int screenY = clientOrigin.Y + tabBarPx  + visibleRow * cellH;

        Logger.Debug("ConsoleCursor",
            $"dpiScale={dpiScale:F2} col={col} visRow={visibleRow}/{viewportRows} cell={cellW}x{cellH} tabBarPx={tabBarPx} clientOrigin=({clientOrigin.X},{clientOrigin.Y}) \u2192 screen=({screenX},{screenY})");
        return new Point(screenX, screenY);
    }

    // ── 进程树搜索 ────────────────────────────────────────────────────────────

    private static uint FindShellChildPid(uint parentPid)
    {
        IntPtr snap = Win32.CreateToolhelp32Snapshot(Win32.TH32CS_SNAPPROCESS, 0);
        if (snap == Win32.INVALID_HANDLE_VALUE) return 0;

        try
        {
            // 建立 parentPid → [childPid] 映射，只关心深度 2 以内
            var directChildren  = new List<uint>();
            var depth2Children  = new List<uint>();

            var entry = new Win32.PROCESSENTRY32
            {
                dwSize = (uint)Marshal.SizeOf<Win32.PROCESSENTRY32>()
            };

            if (!Win32.Process32First(snap, ref entry)) return 0;

            do
            {
                if (entry.th32ParentProcessID == parentPid)
                    directChildren.Add(entry.th32ProcessID);
            } while (Win32.Process32Next(snap, ref entry));

            // 第二次遍历找孙子进程
            entry.dwSize = (uint)Marshal.SizeOf<Win32.PROCESSENTRY32>();
            if (Win32.Process32First(snap, ref entry))
            {
                do
                {
                    if (directChildren.Contains(entry.th32ParentProcessID))
                        depth2Children.Add(entry.th32ProcessID);
                } while (Win32.Process32Next(snap, ref entry));
            }

            // 在所有子/孙进程中找第一个已知 shell
            var allCandidates = new List<(uint pid, int priority)>();

            entry.dwSize = (uint)Marshal.SizeOf<Win32.PROCESSENTRY32>();
            if (Win32.Process32First(snap, ref entry))
            {
                do
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(entry.szExeFile);
                    if (!KnownShells.Contains(name)) continue;

                    if (directChildren.Contains(entry.th32ProcessID))
                        allCandidates.Add((entry.th32ProcessID, 0));   // 直接子进程优先
                    else if (depth2Children.Contains(entry.th32ProcessID))
                        allCandidates.Add((entry.th32ProcessID, 1));
                } while (Win32.Process32Next(snap, ref entry));
            }

            if (allCandidates.Count == 0) return 0;

            // 取优先级最高（priority 最小）的
            allCandidates.Sort((a, b) => a.priority.CompareTo(b.priority));
            return allCandidates[0].pid;
        }
        catch (Exception ex)
        {
            Logger.Warn("ConsoleCursorService", $"进程枚举失败: {ex.Message}");
            return 0;
        }
        finally
        {
            Win32.CloseHandle(snap);
        }
    }

    // ── Console 光标信息 ──────────────────────────────────────────────────────

    private static bool TryGetConsoleCursorInfo(
        uint shellPid,
        out short col, out short visibleRow,
        out short cellW, out short cellH,
        out short viewportRows)
    {
        col = visibleRow = cellW = cellH = viewportRows = 0;

        lock (_consoleLock)
        {
            // WPF 进程通常没有 console，直接 attach
            bool attached = Win32.AttachConsole(shellPid);
            if (!attached)
            {
                Logger.Warn("ConsoleCursorService",
                    $"AttachConsole({shellPid}) 失败，错误码={Marshal.GetLastWin32Error()}");
                return false;
            }

            try
            {
                IntPtr hOut = Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE);
                if (hOut == IntPtr.Zero || hOut == Win32.INVALID_HANDLE_VALUE)
                    return false;

                // 光标行列（绝对缓冲坐标）
                if (!Win32.GetConsoleScreenBufferInfo(hOut, out var bufInfo))
                    return false;

                col          = bufInfo.dwCursorPosition.X;
                // 视口相对行（消除滚动偏移）
                visibleRow   = (short)(bufInfo.dwCursorPosition.Y - bufInfo.srWindow.Top);
                if (visibleRow < 0) visibleRow = 0;
                viewportRows = (short)(bufInfo.srWindow.Bottom - bufInfo.srWindow.Top + 1);
                if (viewportRows <= 0) viewportRows = 24;  // 保守兜底

                // 字符单元格像素大小
                var fontInfo = new Win32.CONSOLE_FONT_INFOEX
                {
                    cbSize = (uint)Marshal.SizeOf<Win32.CONSOLE_FONT_INFOEX>()
                };

                if (Win32.GetCurrentConsoleFontEx(hOut, false, ref fontInfo))
                {
                    cellW = fontInfo.dwFontSize.X;
                    cellH = fontInfo.dwFontSize.Y;
                }

                // 兜底：字体尺寸异常时使用合理默认值
                if (cellW <= 0) cellW = 8;
                if (cellH <= 0) cellH = 16;

                return true;
            }
            finally
            {
                Win32.FreeConsole();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
