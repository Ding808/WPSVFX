using System.Runtime.InteropServices;
using PowerFx.Helper.Models;
using PowerFx.Helper.Native;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 安装低级键盘钩子（WH_KEYBOARD_LL），将按键转换为 <see cref="KeyEffectEvent"/> 发布。
/// </summary>
public sealed class KeyboardHookService : IDisposable
{
    public event Action<KeyEffectEvent>? KeyDown;

    private HookInterop? _hook;
    private bool _disposed;

    public void Install()
    {
        _hook = new HookInterop(Win32.WH_KEYBOARD_LL, HookCallback);
        _hook.Install();
    }

    public void Uninstall()
    {
        _hook?.Uninstall();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == Win32.HC_ACTION &&
            (wParam == Win32.WM_KEYDOWN || wParam == Win32.WM_SYSKEYDOWN))
        {
            try
            {
                var kbStruct = Marshal.PtrToStructure<Win32.KBDLLHOOKSTRUCT>(lParam);
                var evt = ClassifyKeyEvent(kbStruct.vkCode);
                if (evt != null)
                {
                    KeyDown?.Invoke(evt);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("KeyboardHookService", $"处理按键事件异常: {ex.Message}");
            }
        }

        return _hook!.CallNext(nCode, wParam, lParam);
    }

    private static KeyEffectEvent? ClassifyKeyEvent(uint vk)
    {
        var type = vk switch
        {
            Win32.VK_BACK   => KeyEventType.Backspace,
            Win32.VK_DELETE => KeyEventType.Delete,
            Win32.VK_RETURN => KeyEventType.Enter,
            0x09            => KeyEventType.Tab,   // VK_TAB
            0x25            => KeyEventType.Arrow, // 左
            0x26            => KeyEventType.Arrow, // 上
            0x27            => KeyEventType.Arrow, // 右
            0x28            => KeyEventType.Arrow, // 下
            _               => IsCtrlA(vk) ? KeyEventType.CtrlA : KeyEventType.Normal
        };

        return new KeyEffectEvent { EventType = type, VirtualKey = vk };
    }

    private static bool IsCtrlA(uint vk)
    {
        if (vk != Win32.VK_KEY_A) return false;
        var ctrlState = Win32.GetKeyState(Win32.VK_CONTROL);
        return (ctrlState & 0x8000) != 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _hook?.Dispose();
    }
}
