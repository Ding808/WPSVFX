namespace PowerFx.Helper.Models;

/// <summary>
/// 按键效果事件类型枚举。
/// </summary>
public enum KeyEventType
{
    Normal,
    Backspace,
    Delete,
    Enter,
    CtrlA
}

/// <summary>
/// 从键盘钩子产生的按键效果事件。
/// </summary>
public sealed class KeyEffectEvent
{
    public KeyEventType EventType   { get; init; }
    public uint         VirtualKey  { get; init; }

    public override string ToString()
        => $"KeyEffectEvent({EventType}, vk=0x{VirtualKey:X2})";
}
