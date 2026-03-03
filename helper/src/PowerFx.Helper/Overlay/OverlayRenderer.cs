using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Overlay;

/// <summary>
/// 使用 <see cref="CompositionTarget.Rendering"/> 驱动帧循环，
/// 更新粒子数据并将粒子渲染为 WPF Ellipse 到 Canvas。
///
/// 为避免频繁 DOM 操作，使用对象池复用 Ellipse 元素。
/// </summary>
public sealed class OverlayRenderer
{
    private readonly Canvas _canvas;
    private readonly ParticleEmitter _emitter;
    private readonly FrameClock _clock = new();

    // Ellipse 对象池（避免频繁创建 WPF 元素）
    private readonly Queue<Ellipse> _ellipsePool = new();
    private readonly List<(Particle particle, Ellipse ellipse)> _active = new();

    private bool _running;

    public OverlayRenderer(Canvas canvas, ParticleEmitter emitter)
    {
        _canvas  = canvas;
        _emitter = emitter;
    }

    public void Start()
    {
        _running = true;
        CompositionTarget.Rendering += OnRender;
    }

    public void Stop()
    {
        _running = false;
        CompositionTarget.Rendering -= OnRender;
        ReturnAllToPool();
    }

    private void OnRender(object? sender, EventArgs e)
    {
        if (!_running) return;

        double dt = _clock.Tick();

        // 接收新发射的粒子
        while (_emitter.TryDequeue(out var p))
        {
            var ellipse = RentEllipse(p);
            _active.Add((p, ellipse));
            _canvas.Children.Add(ellipse);
        }

        // 更新现有粒子
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var (particle, ellipse) = _active[i];
            particle.Update(dt);

            if (!particle.IsAlive)
            {
                _canvas.Children.Remove(ellipse);
                ReturnEllipseToPool(ellipse);
                _active.RemoveAt(i);
                continue;
            }

            // 更新位置和透明度
            Canvas.SetLeft(ellipse, particle.X - particle.Size / 2.0);
            Canvas.SetTop (ellipse, particle.Y - particle.Size / 2.0);
            ellipse.Opacity = particle.Alpha;
            ellipse.Width   = particle.Size;
            ellipse.Height  = particle.Size;
        }
    }

    private Ellipse RentEllipse(Particle p)
    {
        Ellipse el;
        if (_ellipsePool.Count > 0)
        {
            el = _ellipsePool.Dequeue();
        }
        else
        {
            el = new Ellipse { IsHitTestVisible = false };
        }

        el.Fill    = new SolidColorBrush(p.Color);
        el.Width   = p.Size;
        el.Height  = p.Size;
        el.Opacity = 1.0;
        return el;
    }

    private void ReturnEllipseToPool(Ellipse el)
    {
        _ellipsePool.Enqueue(el);
    }

    private void ReturnAllToPool()
    {
        foreach (var (_, el) in _active)
        {
            _canvas.Children.Remove(el);
            ReturnEllipseToPool(el);
        }
        _active.Clear();
    }
}
