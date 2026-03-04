using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using PowerFx.Helper.Models;

namespace PowerFx.Helper.Overlay;

/// <summary>
/// 粒子发射器，带对象池，避免频繁 GC。
/// 线程安全：可在非 UI 线程调用 <see cref="Emit"/>。
/// <see cref="TryDequeue"/> 由渲染帧循环在 UI 线程消费。
/// </summary>
public sealed class ParticleEmitter
{
    // 对象池（存放已回收的 Particle）
    private readonly ConcurrentBag<Particle> _pool = new();

    // 待渲染队列（生产者→消费者跨线程）
    private readonly ConcurrentQueue<Particle> _pending = new();

    private static readonly Random Rng = new();

    /// <summary>
    /// 在指定位置发射一批粒子。
    /// </summary>
    public void Emit(Point origin, ParticlePreset preset)
    {
        if (preset.Name == "Enter")
        {
            EmitLightning(origin, preset);
            return;
        }

        for (int i = 0; i < preset.Count; i++)
        {
            var p = RentOrCreate();

            double angle = Rng.NextDouble() * Math.PI * 2.0;
            double speed = preset.MinSpeed + Rng.NextDouble() * (preset.MaxSpeed - preset.MinSpeed);

            double vx = Math.Cos(angle) * speed;
            double vy = Math.Sin(angle) * speed * (preset.Upward && !preset.Implode ? -1.5 : 1.0);
            
            double pX = origin.X;
            double pY = origin.Y;

            double lifetime = preset.MinLifetime + Rng.NextDouble() * (preset.MaxLifetime - preset.MinLifetime);

            if (preset.Implode)
            {
                // 如果是收缩，则初始位置偏移，并让速度指向中心
                pX += vx * lifetime * 0.8;
                pY += vy * lifetime * 0.8;
                vx = -vx;
                vy = -vy;
            }

            var color = InterpolateColor(preset.Palette, (double)i / preset.Count);

            p.Reset(
                x:        pX,
                y:        pY,
                vx:       vx,
                vy:       vy,
                color:    color,
                size:     preset.MinSize + Rng.NextDouble() * (preset.MaxSize - preset.MinSize),
                lifetime: lifetime,
                gravity:  preset.Gravity,
                shrink:   preset.Shrink);

            _pending.Enqueue(p);
        }
    }

    public void EmitLightning(Point origin, ParticlePreset preset)
    {
        // 闪电：从光标向上方（或横向）生成折线，使用 Particle 的 TargetX/TargetY 保存线段
        int branches = 2 + Rng.Next(3);
        for (int b = 0; b < branches; b++)
        {
            double curX = origin.X;
            double curY = origin.Y;
            int segments = 5 + Rng.Next(5);
            var color = preset.Palette[Rng.Next(preset.Palette.Length)];
            
            for (int i = 0; i < segments; i++)
            {
                double nextX = curX + (Rng.NextDouble() - 0.5) * 60;
                double nextY = curY - (10 + Rng.NextDouble() * 30); // 向上蔓延

                var p = RentOrCreate();
                p.TargetReset(
                    ParticleType.Lightning,
                    curX, curY, nextX, nextY,
                    color,
                    size: preset.MinSize + Rng.NextDouble() * 2,
                    lifetime: preset.MinLifetime + Rng.NextDouble() * 0.1);
                
                _pending.Enqueue(p);

                curX = nextX;
                curY = nextY;
            }
        }
    }

    public void EmitSlash(Point start, Point end)
    {
        // 水果忍者刀光特效：在两点之间生成一条带有拖尾感觉的线
        var p = RentOrCreate();
        p.TargetReset(
            ParticleType.Slash,
            start.X, start.Y, end.X, end.Y,
            Colors.White, // 核心白色
            size: 4.0,
            lifetime: 0.3); // 闪现 0.3s
        _pending.Enqueue(p);

        // 蓝青色余晖
        var p2 = RentOrCreate();
        p2.TargetReset(
            ParticleType.Slash,
            start.X, start.Y, end.X, end.Y,
            Color.FromArgb(180, 50, 200, 255),
            size: 8.0,
            lifetime: 0.4);
        _pending.Enqueue(p2);
    }

    /// <summary>
    /// 在当前字符位置发射一个静态发光方块。
    /// </summary>
    public void EmitStaticBlock(Point center, double width, double height, Color color, double lifetime)
    {
        var p = RentOrCreate();
        // 居中绘制，算出左上角 X Y (假设 OverlayRenderer 里直接以 X Y 为中心或者左上角，后续 Renderer 注意处理)
        p.StaticBlockReset(center.X, center.Y, color, width, height, lifetime);
        _pending.Enqueue(p);
    }

    /// <summary>
    /// 渲染帧循环消费粒子。返回 false 表示队列为空。
    /// </summary>
    public bool TryDequeue(out Particle particle)
        => _pending.TryDequeue(out particle!);

    /// <summary>
    /// 粒子死亡后由渲染器归还到对象池。
    /// </summary>
    public void Return(Particle p) => _pool.Add(p);

    private Particle RentOrCreate()
        => _pool.TryTake(out var p) ? p : new Particle();

    private static Color InterpolateColor(Color[] palette, double t)
    {
        if (palette.Length == 0) return Colors.White;
        if (palette.Length == 1) return palette[0];

        double scaled = t * (palette.Length - 1);
        int    idx    = Math.Min((int)scaled, palette.Length - 2);
        double frac   = scaled - idx;

        var c1 = palette[idx];
        var c2 = palette[idx + 1];

        return Color.FromArgb(
            (byte)(c1.A + (c2.A - c1.A) * frac),
            (byte)(c1.R + (c2.R - c1.R) * frac),
            (byte)(c1.G + (c2.G - c1.G) * frac),
            (byte)(c1.B + (c2.B - c1.B) * frac));
    }
}
