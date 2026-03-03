using System.Windows;
using PowerFx.Helper.Models;
using PowerFx.Helper.Overlay;
using PowerFx.Helper.Services;
using PowerFx.Helper.Utils;
using WpfPoint = System.Windows.Point;

namespace PowerFx.Helper;

/// <summary>
/// 组装所有服务，管理整体生命周期。
/// 职责：依赖注入 → 服务启动 → 事件连接 → 协调关闭。
/// </summary>
public sealed class MainBootstrapper : IDisposable
{
    // ── 服务实例 ─────────────────────────────────────────────
    private ForegroundTerminalService? _foreground;
    private TerminalWindowTracker? _tracker;
    private KeyboardHookService? _keyboard;
    private MouseHookService? _mouse;
    private SoundService? _sound;
    private WindowShakeService? _shake;
    private ConsoleCursorService? _consoleCursor;
    private FontMetricsReader? _fontMetrics;
    private CaretTrackerService? _caretTracker;
    private SelectionTrailService? _selectionTrail;
    private ParticleEffectRouter? _particleRouter;
    private OverlayService? _overlay;

    private OverlayWindow? _overlayWindow;
    private bool _disposed;

    // ── 启动 ─────────────────────────────────────────────────

    public void Start(Application app)
    {
        Logger.Info("Bootstrapper", "初始化服务...");

        // 创建 overlay 窗口（必须在 STA 线程）
        _overlayWindow = new OverlayWindow();

        // 初始化各服务
        _foreground     = new ForegroundTerminalService();
        _tracker        = new TerminalWindowTracker(_foreground);
        _sound          = new SoundService();
        _shake          = new WindowShakeService();
        _consoleCursor  = new ConsoleCursorService();
        _fontMetrics    = new FontMetricsReader();
        _caretTracker   = new CaretTrackerService(_foreground!, _consoleCursor, _fontMetrics);
        _selectionTrail = new SelectionTrailService();
        _overlay        = new OverlayService(_overlayWindow, _tracker);
        _particleRouter = new ParticleEffectRouter(_overlayWindow);
        _keyboard       = new KeyboardHookService();
        _mouse          = new MouseHookService();

        // Overlay hwnd 就绪后立即通知前台检测服务排除自身
        _overlayWindow.HwndReady += hwnd => _foreground!.OverlayHwnd = hwnd;

        // 连接事件
        WireEvents();

        // 启动服务
        _foreground.Start();
        _tracker.Start();
        _keyboard.Install();
        _mouse.Install();
        _sound.LoadSounds();
        _overlay.Start();

        Logger.Info("Bootstrapper", "所有服务已启动");
    }

    // ── 事件连接 ─────────────────────────────────────────────

    private void WireEvents()
    {
        // 键盘事件 → 按键音效 + 粒子 + shake
        _keyboard!.KeyDown += OnKeyDown;

        // 鼠标事件 → 选区拖尾检测
        _mouse!.MouseMove += _selectionTrail!.OnMouseMove;
        _mouse.MouseButtonDown += _selectionTrail!.OnMouseButtonDown;
        _mouse.MouseButtonUp += _selectionTrail!.OnMouseButtonUp;
        _selectionTrail!.SelectionStarted += OnSelectionStarted;

        // 前台窗口变化 → overlay 显隐
        _foreground!.ActiveStateChanged += _overlay!.OnTerminalActiveChanged;
    }

    private void OnKeyDown(KeyEffectEvent evt)
    {
        if (!_foreground!.IsTerminalActive)
        {
            Logger.Debug("Bootstrapper", $"OnKeyDown 忽略：WT 未激活（key={evt.EventType}）");
            return;
        }

        Logger.Debug("Bootstrapper", $"OnKeyDown 触发 key={evt.EventType}");

        // 播放音效
        _sound!.PlayForEvent(evt);

        // 获取光标屏幕坐标（AttachConsole 控制台 API，失败时返回 null 降级）
        WpfPoint? caretPos = _caretTracker!.GetCaretScreenPoint();
        Logger.Debug("Bootstrapper", $"CaretPos = {(caretPos.HasValue ? $"{caretPos.Value.X:F0},{caretPos.Value.Y:F0}" : "null")}");

        // 触发粒子（带光标位置）
        _particleRouter!.Route(evt, caretPos);

        // 连续抖动：按键类型决定幅度，停键后 150ms 自动恢复原位
        var hwnd = _foreground.CurrentTerminalHandle;
        int amplitude = evt.EventType switch
        {
            KeyEventType.Backspace => _shake!.DeleteAmplitude,
            KeyEventType.Delete    => _shake!.DeleteAmplitude,
            KeyEventType.Enter     => _shake!.EnterAmplitude,
            _                      => _shake!.NormalAmplitude
        };
        _shake!.OnKeyPress(hwnd, amplitude);
    }

    private void OnSelectionStarted(System.Drawing.Point startPoint)
    {
        if (!_foreground!.IsTerminalActive) return;
        _sound!.PlaySelect();
        _particleRouter!.EmitSelectionStart(startPoint);
    }

    // ── 停止 ─────────────────────────────────────────────────

    public void Stop()
    {
        Logger.Info("Bootstrapper", "正在关闭所有服务...");
        _keyboard?.Uninstall();
        _mouse?.Uninstall();
        _foreground?.Stop();
        _tracker?.Stop();
        _overlay?.Stop();
        _shake?.Dispose();
        _sound?.Dispose();
        Logger.Info("Bootstrapper", "关闭完成");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _sound?.Dispose();
    }
}
