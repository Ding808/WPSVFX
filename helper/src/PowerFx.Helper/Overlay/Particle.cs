using System.Windows.Media;

namespace PowerFx.Helper.Overlay;

public enum ParticleType
{
    Circle,
    Lightning,
    Slash,
    StaticBlock // 新增静态方块类型
}

/// <summary>
/// 单个粒子数据。使用值类型避免 GC 压力（但保留 class 以便对象池复用引用）。
/// </summary>
public sealed class Particle
{
    public ParticleType Type { get; set; } = ParticleType.Circle;

    // ── 位置 & 速度
    public double X  { get; set; }
    public double Y  { get; set; }
    public double TargetX { get; set; } // 用于线型特效
    public double TargetY { get; set; }
    public double Vx { get; set; }
    public double Vy { get; set; }

    // ── 视觉属性
    public double Size  { get; set; } = 4.0;
    public double MaxSize { get; set; } = 4.0; // 用于收缩或放大特效
    public Color  Color { get; set; } = Colors.White;
    public double Alpha { get; set; } = 1.0;

    // ── 生命周期
    public double Lifetime    { get; set; } = 0.6; // 秒
    public double Age         { get; set; } = 0.0;
    public bool   IsAlive     => Age < Lifetime;

    // ── 物理参数
    public double Gravity   { get; set; } = 200.0; // px/s²
    public double FrictionX { get; set; } = 0.92;  // 每帧速度缩放
    public bool   Shrink    { get; set; } = false; // 是否在生命周期内收缩

    /// <summary>初始化粒子状态（对象池复用时调用）。</summary>
    public void Reset(
        double x, double y,
        double vx, double vy,
        Color color,
        double size     = 4.0,
        double lifetime = 0.6,
        double gravity  = 200.0,
        bool shrink     = false)
    {
        Type = ParticleType.Circle;
        X = x; Y = y;
        TargetX = x; TargetY = y;
        Vx = vx; Vy = vy;
        Color    = color;
        Size     = size;
        MaxSize  = size;
        Lifetime = lifetime;
        Gravity  = gravity;
        Shrink   = shrink;
        Age      = 0.0;
        Alpha    = 1.0;
    }

    public void TargetReset(ParticleType type, double startX, double startY, double endX, double endY, Color color, double size, double lifetime)
    {
        Type = type;
        X = startX; Y = startY;
        TargetX = endX; TargetY = endY;
        Vx = 0; Vy = 0;
        Color = color;
        Size = size;
        MaxSize = size;
        Lifetime = lifetime;
        Gravity = 0;
        Shrink = false;
        Age = 0.0;
        Alpha = 1.0;
    }

    public void StaticBlockReset(double x, double y, Color color, double width, double height, double lifetime)
    {
        Type = ParticleType.StaticBlock;
        X = x; Y = y;
        TargetX = width; TargetY = height; // 复用 TargetX/Y 存储宽高
        Vx = 0; Vy = 0;
        Color = color;
        Size = width;
        MaxSize = width;
        Lifetime = lifetime;
        Gravity = 0;
        Shrink = false;
        Age = 0.0;
        Alpha = 0.8; // 微微透明
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

        // 特效如果需要坐标跟随 TargetX/Y 偏移也可以在这里处理，这里简单处理线型

        // 透明度随生命抛物线或线性衰减
        Alpha = Math.Max(0.0, 1.0 - Age / Lifetime);

        // 收缩动画
        if (Shrink && Type != ParticleType.StaticBlock)
        {
            Size = MaxSize * (1.0 - Age / Lifetime);
            if (Size < 0) Size = 0;
        }

        // 静态方块特定的透明度控制（闪烁或淡出）
        if (Type == ParticleType.StaticBlock)
        {
            // 例如前半生较亮，后半生淡出
            Alpha = 0.8 * Math.Max(0.0, 1.0 - (Age / Lifetime));
        }
    }
}
