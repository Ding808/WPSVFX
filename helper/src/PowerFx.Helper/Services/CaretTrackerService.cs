using System.Windows;
using System.Windows.Automation;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 双策略光标追踪：
///   1. UIAutomation TextPattern（直接向 WT 查询光标矩形，最精确）
///   2. ConsoleCursorService（ConPTY 坐标换算，兜底）
///
/// UIA 方案：通过窗口句柄找到 WT 的 UIA 文本元素，调用 TextPattern.GetSelection()
/// 获取光标范围的屏幕矩形，无需任何坐标计算。
/// WT 1.18+ 对此有较好支持；旧版或无选区时自动降级。
/// </summary>
public sealed class CaretTrackerService
{
    private readonly ForegroundTerminalService _foreground;
    private readonly ConsoleCursorService      _consoleCursor;
    private readonly FontMetricsReader         _fontMetrics;

    private Point? _cached;
    private long   _lastPollTick;
    private const int PollIntervalMs = 50;

    // UIA 文本元素缓存（避免每次重新遍历树）
    private AutomationElement? _cachedTextElement;
    private IntPtr             _cachedTextElementHwnd;

    public CaretTrackerService(
        ForegroundTerminalService foreground,
        ConsoleCursorService      consoleCursor,
        FontMetricsReader?        fontMetrics = null)
    {
        _foreground    = foreground;
        _consoleCursor = consoleCursor;
        _fontMetrics   = fontMetrics ?? new FontMetricsReader();
    }

    public Point? GetCaretScreenPoint()
    {
        long now = Environment.TickCount64;
        if (now - _lastPollTick < PollIntervalMs)
            return _cached;

        _lastPollTick = now;
        _cached       = QueryCaret();
        return _cached;
    }

    private Point? QueryCaret()
    {
        var hwnd = _foreground.CurrentTerminalHandle;
        if (hwnd == IntPtr.Zero) return null;

        // ── 策略 1：UIAutomation（直接精确）─────────────────────
        var uiaResult = TryGetCaretViaUia(hwnd);
        if (uiaResult.HasValue)
        {
            Logger.Debug("CaretTracker", $"UIA 光标: ({uiaResult.Value.X:F0},{uiaResult.Value.Y:F0})");
            return uiaResult;
        }

        // ── 策略 2：ConPTY 坐标换算（兜底）──────────────────────
        try
        {
            var m = _fontMetrics.Read();
            var screenPt = _consoleCursor.GetCursorScreenPoint(
                hwnd,
                tabBarHeight   : m.TabBarHeight,
                contentPadding : m.ContentPadding);

            if (screenPt is null) return null;

            // 物理像素 → WPF 逻辑像素
            var src = System.Windows.PresentationSource.FromVisual(
                System.Windows.Application.Current.MainWindow);
            if (src?.CompositionTarget is null)
                return new Point(screenPt.Value.X, screenPt.Value.Y);

            double lx = screenPt.Value.X * src.CompositionTarget.TransformFromDevice.M11;
            double ly = screenPt.Value.Y * src.CompositionTarget.TransformFromDevice.M22;
            Logger.Debug("CaretTracker", $"ConPTY 光标: ({lx:F0},{ly:F0})");
            return new Point(lx, ly);
        }
        catch (Exception ex)
        {
            Logger.Warn("CaretTracker", $"ConPTY 策略失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 通过窗口句柄找到 WT 的 UIA 文本元素，返回光标的 WPF 逻辑像素坐标。
    /// WT 直接提供屏幕矩形，无需任何数学换算。
    /// </summary>
    private Point? TryGetCaretViaUia(IntPtr hwnd)
    {
        // 发现部分版本的 Windows Terminal 的 UIA 在未选取任何文字时会返回固定值（如窗口左上角 50,111 等），
        // 导致特效卡在左上角不动。这里直接禁用 UIA 策略，全面回退到精算后的 ConPTY 策略，
        // ConPTY 现在会根据 WT 物理尺寸反推实际字符宽高，精确度极高。
        return null;
        /*
        try
        {
            // 缓存：只有当窗口句柄改变时才重新搜索文本元素
            AutomationElement? textElem = null;
            if (hwnd == _cachedTextElementHwnd && _cachedTextElement != null)
            {
                textElem = _cachedTextElement;
            }
            else
            {
                var wtElem = AutomationElement.FromHandle(hwnd);
                if (wtElem == null) return null;

                // 找 WT 窗口树里第一个支持 TextPattern 的子元素
                var cond = new PropertyCondition(
                    AutomationElement.IsTextPatternAvailableProperty, true);
                textElem = wtElem.FindFirst(TreeScope.Descendants, cond);

                _cachedTextElement     = textElem;
                _cachedTextElementHwnd = hwnd;
            }

            if (textElem == null) return null;

            if (!textElem.TryGetCurrentPattern(TextPattern.Pattern, out var patObj))
                return null;

            var tp        = (TextPattern)patObj;
            var selection = tp.GetSelection();

            // 有选区时取选区起点；无选区时 GetSelection 返回长度为 0 的范围（即光标）
            if (selection.Length == 0) return null;

            var rects = selection[0].GetBoundingRectangles();
            if (rects.Length == 0) return null;

            // UIAutomation 的 GetBoundingRectangles 通常在 Windows Forms 和 WPF 对接口之间经过缩放修正
            // 测试证明直接使用 rect 返回的值作为逻辑单位就已足够。如果乘 M11 则会偏左上（缩水了），
            // 直接返回原坐标：
            return new Point(rects[0].Left, rects[0].Top);
        }
        catch
        {
            // COM 异常、WT 版本不支持等，静默降级
            _cachedTextElement     = null;
            _cachedTextElementHwnd = IntPtr.Zero;
            return null;
        }
        */
    }
}

