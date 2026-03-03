using System.Drawing;
using System.Timers;
using PowerFx.Helper.Models;
using PowerFx.Helper.Native;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 持续跟踪 Windows Terminal 窗口的位置和大小，
/// 供 overlay 同步位置使用。
/// </summary>
public sealed class TerminalWindowTracker : IDisposable
{
    public event Action<TerminalWindowState>? WindowStateChanged;

    public TerminalWindowState CurrentState { get; private set; } = TerminalWindowState.Inactive;

    private readonly ForegroundTerminalService _foreground;
    private readonly System.Timers.Timer _trackTimer;
    private bool _disposed;

    public TerminalWindowTracker(ForegroundTerminalService foreground, int intervalMs = 50)
    {
        _foreground = foreground;
        _trackTimer = new System.Timers.Timer(intervalMs);
        _trackTimer.AutoReset = true;
        _trackTimer.Elapsed += OnTrackTick;
    }

    public void Start()
    {
        _foreground.ActiveStateChanged += OnActiveStateChanged;
        _trackTimer.Start();
    }

    public void Stop()
    {
        _trackTimer.Stop();
        _foreground.ActiveStateChanged -= OnActiveStateChanged;
    }

    private void OnActiveStateChanged(bool isActive, IntPtr hwnd)
    {
        if (!isActive)
        {
            CurrentState = TerminalWindowState.Inactive;
            WindowStateChanged?.Invoke(CurrentState);
        }
    }

    private void OnTrackTick(object? sender, ElapsedEventArgs e)
    {
        var hwnd = _foreground.CurrentTerminalHandle;
        if (hwnd == IntPtr.Zero || !_foreground.IsTerminalActive) return;

        try
        {
            var rect = WindowInterop.GetWindowRect(hwnd);
            if (rect == null) return;

            bool minimized = Win32.IsIconic(hwnd);

            var newState = new TerminalWindowState
            {
                Handle    = hwnd,
                Bounds    = rect.Value,
                IsActive  = true,
                Minimized = minimized
            };

            if (!newState.Equals(CurrentState))
            {
                CurrentState = newState;
                WindowStateChanged?.Invoke(newState);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("TerminalWindowTracker", $"跟踪异常: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _trackTimer.Dispose();
    }
}
