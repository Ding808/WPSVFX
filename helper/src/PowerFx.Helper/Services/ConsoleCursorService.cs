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

    // Console 宿主进程：直接持有 ConPTY 缓冲，当找不到 shell 时作为回退目标
    private static readonly HashSet<string> ConsoleHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "OpenConsole", "conhost"
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

        // 2. 找 shell / console-host 子进程
        uint shellPid = FindShellChildPid(wtPid);
        if (shellPid == 0) return null;

        // 3. 通过 Console API 获取光标行列 + 字符单元格大小 + 视口总行数
        if (!TryGetConsoleCursorInfo(shellPid,
                out short col, out short visibleRow,
                out short cellW, out short cellH,
                out short viewportRows, out short viewportCols))
            return null;

        // 4. WT 窗口客户区矩形 + 客户区原点（屏幕坐标）
        if (!Win32.GetClientRect(wtHwnd, out var clientRect)) return null;
        var clientOrigin = new Win32.POINT { X = 0, Y = 0 };
        if (!Win32.ClientToScreen(wtHwnd, ref clientOrigin)) return null;

        int clientH = clientRect.Bottom - clientRect.Top;
        int clientW = clientRect.Right - clientRect.Left;

        double realCellH = cellH;
        double realCellW = cellW;
        if (viewportRows > 0 && viewportCols > 0)
        {
            // Windows Terminal 常常将 ConPTY 字号报为 8x16（不受设置影响）
            // 我们通过物理窗口尺寸反推真实的字符物理像素宽高
            double estimatedCellH = (clientH - tabBarPx) / (double)viewportRows;
            double estimatedCellW = (clientW - paddingPx * 2) / (double)viewportCols;
            
            // 如果推算值比 8x16 合理，则采用推算值
            if (estimatedCellH > 0) realCellH = estimatedCellH;
            if (estimatedCellW > 0) realCellW = estimatedCellW;
        }

        // 5. 换算（全部物理像素）
        // col * realCellW 是光标所在列左边缘；为了让方块对齐，我们将这个坐标视为起点。
        // visibleRow * realCellH 是行顶部；由于有 padding，需要加上。
        // 这里计算出的是字符槽左上角的精确坐标，之后再向右下角施加中心偏移量，避免方块超出或者错位
        double screenX = clientOrigin.X + paddingPx + col * realCellW;
        double screenY = clientOrigin.Y + tabBarPx  + visibleRow * realCellH;

        // 这里再针对光标本身通常会有稍微向下偏移做一点手工微调
        // 使生成的框体或粒子，其渲染坐标中心在文字的中点
        screenX += realCellW / 2.0;
        screenY += realCellH / 2.0;

        // 微调：由于控制台字体通常有稍微向下的 baseline 偏移，这里 y 轴上拉高或下沉微调
        // 下沉 2 像素刚好匹配文字真正的中央
        screenY += 2;
        screenX += 2; 

        Logger.Debug("ConsoleCursor",
            $"dpiScale={dpiScale:F2} col={col}/{viewportCols} visRow={visibleRow}/{viewportRows} cell={realCellW:F1}x{realCellH:F1} tabBarPx={tabBarPx} clientOrigin=({clientOrigin.X},{clientOrigin.Y}) \u2192 screen=({screenX},{screenY})");
        return new Point((int)screenX, (int)screenY);
    }

    // ── 进程树搜索 ────────────────────────────────────────────────────────────

    private static uint FindShellChildPid(uint wtPid)
    {
        IntPtr snap = Win32.CreateToolhelp32Snapshot(Win32.TH32CS_SNAPPROCESS, 0);
        if (snap == Win32.INVALID_HANDLE_VALUE) return 0;

        try
        {
            // 一次性构建 parentPid → [(childPid, name)] 映射
            var childMap = new Dictionary<uint, List<(uint pid, string name)>>();

            var entry = new Win32.PROCESSENTRY32
            {
                dwSize = (uint)Marshal.SizeOf<Win32.PROCESSENTRY32>()
            };

            if (!Win32.Process32First(snap, ref entry)) return 0;

            do
            {
                var exeName = System.IO.Path.GetFileNameWithoutExtension(entry.szExeFile);
                uint ppid   = entry.th32ParentProcessID;
                if (!childMap.TryGetValue(ppid, out var list))
                    childMap[ppid] = list = new List<(uint, string)>();
                list.Add((entry.th32ProcessID, exeName));
            } while (Win32.Process32Next(snap, ref entry));

            // BFS 遍历进程树，最大深度 4（WT → OpenConsole → shell → 子 shell）
            // 优先返回浅层已知 shell；若全程找不到 shell，则回退到 OpenConsole/conhost
            var queue              = new Queue<(uint pid, string name, int depth)>();
            var visited            = new HashSet<uint> { wtPid };
            var shellCandidates    = new List<(uint pid, int depth)>();
            var consoleHostFallback = new List<(uint pid, int depth)>();

            if (childMap.TryGetValue(wtPid, out var wtChildren))
            {
                foreach (var (pid, name) in wtChildren)
                {
                    if (visited.Add(pid))
                        queue.Enqueue((pid, name, 1));
                }
            }

            while (queue.Count > 0)
            {
                var (pid, name, depth) = queue.Dequeue();

                if (KnownShells.Contains(name))
                {
                    shellCandidates.Add((pid, depth));
                }
                else if (ConsoleHosts.Contains(name))
                {
                    // console 宿主直接持有缓冲，记录备用
                    consoleHostFallback.Add((pid, depth));
                }

                // 继续向下展开（深度上限 4）
                if (depth < 4 && childMap.TryGetValue(pid, out var grandChildren))
                {
                    foreach (var (childPid, childName) in grandChildren)
                    {
                        if (visited.Add(childPid))
                            queue.Enqueue((childPid, childName, depth + 1));
                    }
                }
            }

            // 优先选深度最浅的已知 shell
            if (shellCandidates.Count > 0)
            {
                shellCandidates.Sort((a, b) => a.depth.CompareTo(b.depth));
                Logger.Debug("ConsoleCursor",
                    $"找到 shell PID={shellCandidates[0].pid} depth={shellCandidates[0].depth}");
                return shellCandidates[0].pid;
            }

            // 回退：OpenConsole / conhost 直接持有 ConPTY 缓冲，AttachConsole 仍可用
            if (consoleHostFallback.Count > 0)
            {
                consoleHostFallback.Sort((a, b) => a.depth.CompareTo(b.depth));
                uint fallbackPid = consoleHostFallback[0].pid;
                Logger.Debug("ConsoleCursor",
                    $"未找到 shell，回退到 console host PID={fallbackPid}");
                return fallbackPid;
            }

            Logger.Warn("ConsoleCursor", $"WT PID={wtPid} 进程树中未找到可用目标");
            return 0;
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
        out short viewportRows, out short viewportCols)
    {
        col = visibleRow = cellW = cellH = viewportRows = viewportCols = 0;

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
                {
                    Logger.Warn("ConsoleCursorService",
                        $"GetStdHandle 返回无效句柄 hOut={hOut} 错误码={Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 光标行列（绝对缓冲坐标）
                if (!Win32.GetConsoleScreenBufferInfo(hOut, out var bufInfo))
                {
                    Logger.Warn("ConsoleCursorService",
                        $"GetConsoleScreenBufferInfo 失败，错误码={Marshal.GetLastWin32Error()}");
                    return false;
                }

                col          = bufInfo.dwCursorPosition.X;
                // 视口相对行（消除滚动偏移）
                visibleRow   = (short)(bufInfo.dwCursorPosition.Y - bufInfo.srWindow.Top);
                if (visibleRow < 0) visibleRow = 0;
                viewportRows = (short)(bufInfo.srWindow.Bottom - bufInfo.srWindow.Top + 1);
                viewportCols = (short)(bufInfo.srWindow.Right - bufInfo.srWindow.Left + 1);
                if (viewportRows <= 0) viewportRows = 24;  // 保守兜底
                if (viewportCols <= 0) viewportCols = 80;

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
