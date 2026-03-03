using System.Windows;

namespace PowerFx.Helper.Overlay;

/// <summary>
/// 鼠标拖选拖尾片段数据结构。
/// 每个片段记录起止点和当前可见度，随时间衰减。
/// </summary>
public sealed class TrailSegment
{
    public Point From    { get; set; }
    public Point To      { get; set; }
    public double Alpha  { get; set; } = 1.0;
    public double FadeSpeed { get; set; } = 2.0; // 每秒衰减量

    public bool IsAlive => Alpha > 0.01;

    public void Update(double dt)
    {
        Alpha = Math.Max(0.0, Alpha - FadeSpeed * dt);
    }
}
