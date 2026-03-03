using System.Drawing;
using PowerFx.Helper.Native;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 连续抖动服务。
///
/// 行为：
///   - 每次按键调用 <see cref="OnKeyPress"/>，传入窗口句柄和幅度。
///   - 后台线程以正弦波持续抖动目标窗口。
///   - 停止按键后 <see cref="IdleStopMs"/> 毫秒内自动恢复原始位置。
///   - 精确恢复：抖动开始时捕获原始坐标，停止时还原，不会漂移。
/// </summary>
public sealed class WindowShakeService : IDisposable
{
    // ── 可调参数 ──────────────────────────────────────────────
    public int    IdleStopMs      { get; set; } = 150;   // 停键多久后恢复
    public int    NormalAmplitude { get; set; } = 3;     // 普通按键 px
    public int    DeleteAmplitude { get; set; } = 10;    // Backspace/Delete px
    public int    EnterAmplitude  { get; set; } = 8;     // Enter px

    private const double ShakeHz   = 20.0;  // 振荡频率
    private const int    FrameMs   = 14;    // ~70fps

    // ── 状态（跨线程安全读写）──────────────────
    private IntPtr    _hwnd;             // 实际句柄，只在 ShakeLoop 线程或 lock 内写
    private volatile int  _amplitude;
    private long          _lastKeyTick;
    private Rectangle     _originalBounds;
    private bool          _shaking;
    private double        _phase;
    private readonly object _stateLock = new();

    private volatile bool _running = true;
    private readonly Thread _thread;
    private bool _disposed;

    public WindowShakeService()
    {
        _thread = new Thread(ShakeLoop)
        {
            IsBackground = true,
            Name         = "PowerFx.ShakeLoop"
        };
        _thread.Start();
    }

    /// <summary>
    /// 每次按键时调用，传入目标窗口和本次按键对应的抖动幅度（px）。
    /// 线程安全，可从任意线程调用。
    /// </summary>
    public void OnKeyPress(IntPtr hwnd, int amplitude)
    {
        if (hwnd == IntPtr.Zero || amplitude <= 0) return;

        lock (_stateLock)
        {
            // 目标窗口切换时重新捕获原始位置
            if (!_shaking || hwnd != _hwnd)
            {
                var rect = WindowInterop.GetWindowRect(hwnd);
                if (rect == null) return;
                _originalBounds = rect.Value;
                _hwnd           = hwnd;
                _shaking        = true;
                _phase          = 0;
            }

            // 取最大幅度（让 Delete 的 burst 不会被后续普通键降低）
            _amplitude   = Math.Max(_amplitude, amplitude);
            _lastKeyTick = Environment.TickCount64;
        }
    }

    // ── 后台抖动循环 ──────────────────────────────────────────
    private void ShakeLoop()
    {
        while (_running)
        {
            Thread.Sleep(FrameMs);

            IntPtr  hwnd;
            int     amplitude;
            bool    shaking;
            Rectangle orig;

            lock (_stateLock)
            {
                hwnd      = _hwnd;
                amplitude = _amplitude;
                shaking   = _shaking;
                orig      = _originalBounds;
            }

            if (!shaking || hwnd == IntPtr.Zero) continue;

            long now  = Environment.TickCount64;
            bool idle = (now - _lastKeyTick) > IdleStopMs;

            if (idle)
            {
                // 精确恢复原始位置
                try { WindowInterop.MoveWindow(hwnd, orig.X, orig.Y); }
                catch { /* 窗口可能已关闭 */ }

                lock (_stateLock)
                {
                    _shaking   = false;
                    _amplitude = 0;
                    _phase     = 0;
                }
                continue;
            }

            // 幅度随 idle 进度衰减（按键结束前半程维持，后半程收缩）
            double idleRatio = (now - _lastKeyTick) / (double)IdleStopMs;
            double decayed   = amplitude * Math.Max(0.2, 1.0 - idleRatio);

            _phase += ShakeHz * FrameMs / 1000.0 * Math.PI * 2.0;
            int offsetX = (int)(decayed * Math.Sin(_phase));

            try { WindowInterop.MoveWindow(hwnd, orig.X + offsetX, orig.Y); }
            catch { /* 窗口可能已关闭 */ }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _running  = false;

        // 尝试恢复原位
        lock (_stateLock)
        {
            if (_hwnd != IntPtr.Zero && _shaking)
                try { WindowInterop.MoveWindow(_hwnd, _originalBounds.X, _originalBounds.Y); }
                catch { }
        }
    }
}
