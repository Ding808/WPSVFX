using System.Drawing;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Native;

/// <summary>
/// 封装 SetWindowPos / GetWindowRect 等窗口操作。
/// 保证所有移动操作都有安全边界，防止窗口漂移。
/// </summary>
internal static class WindowInterop
{
    /// <summary>
    /// 获取窗口当前 Rect（屏幕坐标）。
    /// </summary>
    public static Rectangle? GetWindowRect(IntPtr hWnd)
    {
        if (!Win32.GetWindowRect(hWnd, out var rect))
        {
            Logger.Warn("WindowInterop", $"GetWindowRect 失败 hWnd=0x{hWnd:X}");
            return null;
        }
        return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    /// <summary>
    /// 将窗口移动到指定位置，保持大小不变。
    /// </summary>
    public static bool MoveWindow(IntPtr hWnd, int x, int y)
    {
        return Win32.SetWindowPos(
            hWnd,
            IntPtr.Zero,
            x, y, 0, 0,
            Win32.SWP_NOSIZE | Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
    }

    /// <summary>
    /// 将 overlay 窗口设置为透明置顶点击穿透。
    /// </summary>
    public static void SetOverlayStyle(IntPtr hWnd)
    {
        int exStyle = Win32.GetWindowLong(hWnd, Win32.GWL_EXSTYLE);
        exStyle |= Win32.WS_EX_TRANSPARENT | Win32.WS_EX_LAYERED | Win32.WS_EX_NOACTIVATE;
        Win32.SetWindowLong(hWnd, Win32.GWL_EXSTYLE, exStyle);

        Win32.SetWindowPos(
            hWnd,
            Win32.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
    }

    /// <summary>
    /// 将窗口同步到目标窗口的 Rect（overlay 跟随）。
    /// </summary>
    public static bool SyncToTarget(IntPtr overlayHwnd, Rectangle targetRect)
    {
        return Win32.SetWindowPos(
            overlayHwnd,
            Win32.HWND_TOPMOST,
            targetRect.X, targetRect.Y,
            targetRect.Width, targetRect.Height,
            Win32.SWP_NOACTIVATE);
    }

    /// <summary>
    /// 获取窗口类名（用于判断是否为 Windows Terminal）。
    /// </summary>
    public static string GetClassName(IntPtr hWnd)
    {
        var sb = new System.Text.StringBuilder(256);
        Win32.GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
