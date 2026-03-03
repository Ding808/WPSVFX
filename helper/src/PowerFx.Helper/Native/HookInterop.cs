using System.Diagnostics;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Native;

/// <summary>
/// 封装低级键盘 / 鼠标钩子的安装与卸载。
/// </summary>
internal sealed class HookInterop : IDisposable
{
    private readonly int _hookType;
    private readonly Win32.LowLevelProc _proc; // 保持引用，避免 GC
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;

    public HookInterop(int hookType, Win32.LowLevelProc callback)
    {
        _hookType = hookType;
        _proc = callback;
    }

    /// <summary>
    /// 安装钩子。必须在消息循环存在的 STA 线程调用。
    /// </summary>
    public void Install()
    {
        if (_hookId != IntPtr.Zero)
        {
            Logger.Warn("HookInterop", $"钩子 {_hookType} 已安装，忽略重复安装。");
            return;
        }

        using var curProcess = Process.GetCurrentProcess();
        using var curModule  = curProcess.MainModule!;
        var hMod = Win32.GetModuleHandle(curModule.ModuleName);

        _hookId = Win32.SetWindowsHookEx(_hookType, _proc, hMod, 0);

        if (_hookId == IntPtr.Zero)
        {
            int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SetWindowsHookEx 失败，错误码 {err}");
        }

        Logger.Info("HookInterop", $"钩子 {_hookType} 安装成功");
    }

    /// <summary>
    /// 卸载钩子。
    /// </summary>
    public void Uninstall()
    {
        if (_hookId == IntPtr.Zero) return;

        Win32.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        Logger.Info("HookInterop", $"钩子 {_hookType} 已卸载");
    }

    /// <summary>
    /// 将事件传递给下一个钩子（必须在回调中调用）。
    /// </summary>
    public IntPtr CallNext(int nCode, IntPtr wParam, IntPtr lParam)
        => Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Uninstall();
    }
}
