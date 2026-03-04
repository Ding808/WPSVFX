using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Overlay;

/// <summary>
/// 使用 <see cref="CompositionTarget.Rendering"/> 驱动帧循环，
/// 高效渲染粒子。摒弃 Ellipse 以解决卡顿，改用底层 DrawingVisual 渲染。
/// </summary>
public sealed class OverlayRenderer
{
    private readonly Canvas _canvas;
    private readonly ParticleEmitter _emitter;
    private readonly FrameClock _clock = new();

    private readonly List<Particle> _active = new();
    private readonly Queue<Particle> _particlePool = new();

    private bool _running;
    private readonly VisualHost _host;
    private readonly DrawingVisual _visual;

    private class VisualHost : FrameworkElement
    {
        private readonly Visual _child;
        public VisualHost(Visual child)
        {
            _child = child;
            AddVisualChild(child);
            IsHitTestVisible = false;
        }
        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _child;
    }

    private const int MaxActiveParticles = 800; // 同屏最大粒子数，防止极端卡死

    public OverlayRenderer(Canvas canvas, ParticleEmitter emitter)
    {
        _canvas  = canvas;
        _emitter = emitter;
        _visual = new DrawingVisual();
        _host = new VisualHost(_visual);
        _canvas.Children.Add(_host);
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
        _active.Clear();
        using var dc = _visual.RenderOpen();
    }

    private void OnRender(object? sender, EventArgs e)
    {
        if (!_running) return;

        double dt = _clock.Tick();

        // 接收新发射的粒子
        while (_emitter.TryDequeue(out var p))
        {
            if (_active.Count < MaxActiveParticles)
            {
                _active.Add(p);
            }
            else
            {
                // 超出上限直接丢弃回收
                _emitter.Return(p);
            }
        }

        using var dc = _visual.RenderOpen();

        // 如果没有粒子跳过绘制
        if (_active.Count == 0) return;

        // 预定义画刷池（提升性能）
        // 在这里使用高效渲染
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var p = _active[i];
            p.Update(dt);

            if (!p.IsAlive)
            {
                // 高效 O(1) 移除：将末尾元素填补当前空缺
                int lastIdx = _active.Count - 1;
                _active[i] = _active[lastIdx];
                _active.RemoveAt(lastIdx);
                
                _emitter.Return(p);
                continue;
            }

            if (p.Type == ParticleType.Circle)
            {
                Brush brush = GetColorBrush(p.Color, p.Alpha);
                dc.DrawEllipse(brush, null, new Point(p.X, p.Y), p.Size / 2.0, p.Size / 2.0);
            }
            else if (p.Type == ParticleType.StaticBlock)
            {
                // TargetX = width, TargetY = height
                Brush brush = GetColorBrush(p.Color, p.Alpha);
                // X,Y为光标中心点，向四周偏移画出矩形
                double w = p.TargetX;
                double h = p.TargetY;
                var rect = new Rect(p.X - w / 2, p.Y - h / 2, w, h);
                // 绘制发光发亮的微透明方块，外加稍微亮一点的边框
                dc.DrawRectangle(brush, null, rect);
            }
            else if (p.Type == ParticleType.Lightning || p.Type == ParticleType.Slash)
            {
                // 用 Line 绘制特效 (Pen 根据大小临时获取或缓存)
                Pen pen = GetPen(p.Color, p.Alpha, p.Size);
                dc.DrawLine(pen, new Point(p.X, p.Y), new Point(p.TargetX, p.TargetY));
            }
        }
    }

    private readonly Dictionary<Color, Brush> _brushCache = new();
    private readonly Dictionary<(Color, double), Pen> _penCache = new();

    private Brush GetColorBrush(Color baseColor, double alpha)
    {
        // 高效降低透明度开销：直接调整画刷颜色的Alpha层，无需 PushOpacity 创建中间层
        // 为了降低 Cache 字典大小，我们将 Alpha 量化为 0-255 的整数级别（256种）
        byte a = (byte)(Math.Clamp(alpha, 0.0, 1.0) * 255.0);
        Color keyColor = Color.FromArgb(a, baseColor.R, baseColor.G, baseColor.B);

        if (!_brushCache.TryGetValue(keyColor, out var brush))
        {
            brush = new SolidColorBrush(keyColor);
            brush.Freeze();
            _brushCache[keyColor] = brush;
        }
        return brush;
    }

    private Pen GetPen(Color baseColor, double alpha, double thickness)
    {
        byte a = (byte)(Math.Clamp(alpha, 0.0, 1.0) * 255.0);
        Color keyColor = Color.FromArgb(a, baseColor.R, baseColor.G, baseColor.B);
        
        // 线条粗细也进行适度量化，精确到 0.5 像素
        double t = Math.Round(thickness * 2.0) / 2.0;

        var key = (keyColor, t);
        if (!_penCache.TryGetValue(key, out var pen))
        {
            pen = new Pen(GetColorBrush(baseColor, alpha), t);
            pen.Freeze();
            _penCache[key] = pen;
        }
        return pen;
    }
}
