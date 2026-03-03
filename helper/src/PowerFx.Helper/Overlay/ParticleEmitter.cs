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
        for (int i = 0; i < preset.Count; i++)
        {
            var p = RentOrCreate();

            double angle = Rng.NextDouble() * Math.PI * 2.0;
            double speed = preset.MinSpeed + Rng.NextDouble() * (preset.MaxSpeed - preset.MinSpeed);

            double vx = Math.Cos(angle) * speed;
            double vy = Math.Sin(angle) * speed * (preset.Upward ? -1.5 : 1.0);

            var color = InterpolateColor(preset.Palette, (double)i / preset.Count);

            p.Reset(
                x:        origin.X,
                y:        origin.Y,
                vx:       vx,
                vy:       vy,
                color:    color,
                size:     preset.MinSize + Rng.NextDouble() * (preset.MaxSize - preset.MinSize),
                lifetime: preset.MinLifetime + Rng.NextDouble() * (preset.MaxLifetime - preset.MinLifetime),
                gravity:  preset.Gravity);

            _pending.Enqueue(p);
        }
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
