using System.Drawing;

namespace PowerFx.Helper.Models;

/// <summary>
/// Windows Terminal 窗口当前状态快照。
/// 被 <see cref="PowerFx.Helper.Services.TerminalWindowTracker"/> 持续更新并广播。
/// </summary>
public sealed class TerminalWindowState
{
    public static readonly TerminalWindowState Inactive = new()
    {
        IsActive = false,
        Handle   = IntPtr.Zero,
        Bounds   = Rectangle.Empty
    };

    public bool      IsActive  { get; init; }
    public bool      Minimized { get; init; }
    public IntPtr    Handle    { get; init; }
    public Rectangle Bounds    { get; init; }

    public bool Equals(TerminalWindowState? other)
    {
        if (other is null) return false;
        return IsActive  == other.IsActive
            && Minimized == other.Minimized
            && Handle    == other.Handle
            && Bounds    == other.Bounds;
    }
}
