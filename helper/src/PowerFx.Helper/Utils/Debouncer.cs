namespace PowerFx.Helper.Utils;

/// <summary>
/// 通用防抖工具。在 <see cref="Interval"/> 时间内重复调用时，
/// 只有最后一次触发会真正执行 callback。
/// </summary>
public sealed class Debouncer : IDisposable
{
    public TimeSpan Interval { get; set; }

    private Timer? _timer;
    private readonly object _lock = new();
    private bool _disposed;

    public Debouncer(TimeSpan interval)
    {
        Interval = interval;
    }

    /// <summary>
    /// 调用此方法触发防抖，每次调用都会重置计时器。
    /// </summary>
    public void Trigger(Action callback)
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = new Timer(_ => callback(), null, (long)Interval.TotalMilliseconds, Timeout.Infinite);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}

/// <summary>
/// 通用节流工具（Throttle）：在 <see cref="Interval"/> 内只允许执行一次。
/// </summary>
public sealed class Throttle
{
    public TimeSpan Interval { get; set; }
    private long _lastMs;

    public Throttle(TimeSpan interval)
    {
        Interval = interval;
        _lastMs  = 0;
    }

    /// <summary>
    /// 如果距上次执行超过 <see cref="Interval"/>，则执行 callback 并返回 true。
    /// </summary>
    public bool TryInvoke(Action callback)
    {
        long now = Environment.TickCount64;
        if (now - _lastMs < (long)Interval.TotalMilliseconds) return false;
        _lastMs = now;
        callback();
        return true;
    }
}
