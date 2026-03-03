using System.Drawing;
using System.Runtime.InteropServices;
using PowerFx.Helper.Native;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 安装低级鼠标钩子（WH_MOUSE_LL），发布鼠标移动和按键事件。
/// </summary>
public sealed class MouseHookService : IDisposable
{
    public event Action<Point>? MouseMove;
    public event Action<Point>? MouseButtonDown;
    public event Action<Point>? MouseButtonUp;

    private HookInterop? _hook;
    private bool _disposed;

    public void Install()
    {
        _hook = new HookInterop(Win32.WH_MOUSE_LL, HookCallback);
        _hook.Install();
    }

    public void Uninstall()
    {
        _hook?.Uninstall();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == Win32.HC_ACTION)
        {
            try
            {
                var ms = Marshal.PtrToStructure<Win32.MSLLHOOKSTRUCT>(lParam);
                var pt = new Point(ms.pt.X, ms.pt.Y);

                switch ((int)wParam)
                {
                    case Win32.WM_MOUSEMOVE:
                        MouseMove?.Invoke(pt);
                        break;
                    case Win32.WM_LBUTTONDOWN:
                        MouseButtonDown?.Invoke(pt);
                        break;
                    case Win32.WM_LBUTTONUP:
                        MouseButtonUp?.Invoke(pt);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("MouseHookService", $"处理鼠标事件异常: {ex.Message}");
            }
        }

        return _hook!.CallNext(nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _hook?.Dispose();
    }
}
