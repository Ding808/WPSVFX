using System.Windows;
using PowerFx.Helper.Models;
using PowerFx.Helper.Native;
using PowerFx.Helper.Overlay;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 控制 <see cref="OverlayWindow"/> 的生命周期：
/// Overlay 铺满整个虚拟桌面（所有显示器合集），永远从屏幕 (0,0) 开始，
/// 这样屏幕逻辑坐标 == Overlay 本地坐标，无需任何 PointFromScreen 换算。
/// 仅在 Terminal 激活时显示，其他时间隐藏（Overlay 本身点击穿透，不影响其他应用）。
/// </summary>
public sealed class OverlayService : IDisposable
{
    private readonly OverlayWindow _overlay;
    private readonly TerminalWindowTracker _tracker;
    private bool _disposed;

    public OverlayService(OverlayWindow overlay, TerminalWindowTracker tracker)
    {
        _overlay = overlay;
        _tracker = tracker;
    }

    public void Start()
    {
        _tracker.WindowStateChanged += OnWindowStateChanged;

        // 初始铺满虚拟桌面
        Application.Current?.Dispatcher.Invoke(FitToVirtualScreen);

        // 监听显示器配置变化
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        Logger.Info("OverlayService", "已启动");
    }

    public void Stop()
    {
        _tracker.WindowStateChanged -= OnWindowStateChanged;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        Application.Current?.Dispatcher.Invoke(() =>
        {
            try { _overlay.Hide(); }
            catch { /* 窗口可能已关闭 */ }
        });
    }

    public void OnTerminalActiveChanged(bool isActive, IntPtr hwnd)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (isActive)
                _overlay.Show();
            else
                _overlay.Hide();
        });
    }

    private void OnWindowStateChanged(TerminalWindowState state)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (!state.IsActive || state.Minimized)
                _overlay.Hide();
            else if (!_overlay.IsVisible)
                _overlay.Show();
            // 不再跟随 WT 窗口大小移动——Overlay 始终铺满虚拟桌面
        });
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(FitToVirtualScreen);
    }

    /// <summary>
    /// 将 Overlay 设置为覆盖整个虚拟桌面（所有显示器的边界矩形）。
    /// 使用 WPF 逻辑像素，确保 PointFromScreen 不再需要额外偏移换算。
    /// </summary>
    private void FitToVirtualScreen()
    {
        try
        {
            // SystemParameters 返回 WPF 逻辑像素（已按 DPI 换算）
            double left   = SystemParameters.VirtualScreenLeft;
            double top    = SystemParameters.VirtualScreenTop;
            double width  = SystemParameters.VirtualScreenWidth;
            double height = SystemParameters.VirtualScreenHeight;

            _overlay.Left   = left;
            _overlay.Top    = top;
            _overlay.Width  = width;
            _overlay.Height = height;

            Logger.Info("OverlayService",
                $"Overlay 铺满虚拟桌面: ({left},{top}) {width}x{height} 逻辑像素");
        }
        catch (Exception ex)
        {
            Logger.Warn("OverlayService", $"FitToVirtualScreen 失败: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
