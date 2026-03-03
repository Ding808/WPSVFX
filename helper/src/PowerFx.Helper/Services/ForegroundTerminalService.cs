using System.Timers;
using PowerFx.Helper.Models;
using PowerFx.Helper.Native;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 定期轮询前台窗口，判断是否为 Windows Terminal 进程及其窗口类。
/// 当激活状态变化时触发 <see cref="ActiveStateChanged"/> 事件。
/// </summary>
public sealed class ForegroundTerminalService : IDisposable
{
    /// 发射：(isActive, hwnd)
    public event Action<bool, IntPtr>? ActiveStateChanged;

    /// 当前是否处于激活状态
    public bool IsTerminalActive { get; private set; }

    /// 当前 Terminal 窗口句柄（非激活时为 IntPtr.Zero）
    public IntPtr CurrentTerminalHandle { get; private set; }

    // Windows Terminal 的窗口类名
    private const string WtClassName   = "CASCADIA_HOSTING_WINDOW_CLASS";

    private readonly System.Timers.Timer _pollTimer;
    private bool _disposed;

    public ForegroundTerminalService(int pollIntervalMs = 150)
    {
        _pollTimer = new System.Timers.Timer(pollIntervalMs);
        _pollTimer.AutoReset = true;
        _pollTimer.Elapsed += OnPollTick;
    }

    public void Start()
    {
        _pollTimer.Start();
        Logger.Info("ForegroundTerminalService", "前台窗口轮询已启动");
    }

    public void Stop()
    {
        _pollTimer.Stop();
        Logger.Info("ForegroundTerminalService", "前台窗口轮询已停止");
    }

    /// <summary>Overlay 窗口句柄，设置后检测时将其排除在外</summary>
    public IntPtr OverlayHwnd { get; set; }

    private void OnPollTick(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var hwnd = Win32.GetForegroundWindow();

            // 如果前台窗口是我们自己的 Overlay，展示状态不变
            if (hwnd == OverlayHwnd && OverlayHwnd != IntPtr.Zero)
                return;

            bool isWT = hwnd != IntPtr.Zero && IsWindowsTerminal(hwnd);

            if (isWT != IsTerminalActive)
            {
                IsTerminalActive        = isWT;
                CurrentTerminalHandle   = isWT ? hwnd : IntPtr.Zero;
                ActiveStateChanged?.Invoke(isWT, CurrentTerminalHandle);
                Logger.Info("ForegroundTerminalService", $"Terminal 激活状态变化 → {isWT}");
            }
            else if (isWT && hwnd != CurrentTerminalHandle)
            {
                // 同为 WT 但切换了窗口
                CurrentTerminalHandle = hwnd;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("ForegroundTerminalService", $"轮询异常: {ex.Message}");
        }
    }

    private static bool IsWindowsTerminal(IntPtr hwnd)
    {
        var className = WindowInterop.GetClassName(hwnd);
        if (className == WtClassName) return true;

        // 也可能是子窗口，检查进程名
        Win32.GetWindowThreadProcessId(hwnd, out uint pid);
        try
        {
            using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
            return proc.ProcessName.Equals("WindowsTerminal", StringComparison.OrdinalIgnoreCase)
                || proc.ProcessName.Equals("wt", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pollTimer.Dispose();
    }
}
