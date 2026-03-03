using System.Windows.Media;

namespace PowerFx.Helper.Overlay;

/// <summary>
/// 单个粒子数据。使用值类型避免 GC 压力（但保留 class 以便对象池复用引用）。
/// </summary>
public sealed class Particle
{
    // ── 位置 & 速度
    public double X  { get; set; }
    public double Y  { get; set; }
    public double Vx { get; set; }
    public double Vy { get; set; }

    // ── 视觉属性
    public double Size  { get; set; } = 4.0;
    public Color  Color { get; set; } = Colors.White;
    public double Alpha { get; set; } = 1.0;

    // ── 生命周期
    public double Lifetime    { get; set; } = 0.6; // 秒
    public double Age         { get; set; } = 0.0;
    public bool   IsAlive     => Age < Lifetime;

    // ── 物理参数
    public double Gravity   { get; set; } = 200.0; // px/s²
    public double FrictionX { get; set; } = 0.92;  // 每帧速度缩放

    /// <summary>初始化粒子状态（对象池复用时调用）。</summary>
    public void Reset(
        double x, double y,
        double vx, double vy,
        Color color,
        double size     = 4.0,
        double lifetime = 0.6,
        double gravity  = 200.0)
    {
        X = x; Y = y;
        Vx = vx; Vy = vy;
        Color    = color;
        Size     = size;
        Lifetime = lifetime;
        Gravity  = gravity;
        Age      = 0.0;
        Alpha    = 1.0;
    }

    /// <summary>每帧物理更新。</summary>
    public void Update(double dt)
    {
        Age += dt;
        if (!IsAlive) return;

        Vx *= FrictionX;
        Vy += Gravity * dt;

        X += Vx * dt;
        Y += Vy * dt;

        // 透明度随生命线性衰减
        Alpha = Math.Max(0.0, 1.0 - Age / Lifetime);
    }
}
