namespace PowerFx.Helper.Overlay;

/// <summary>
/// 计算每帧 delta-time（秒），供渲染循环物理积分使用。
/// 自动 clamp 防止帧率极低时物理爆炸。
/// </summary>
public sealed class FrameClock
{
    private long _lastTick = Environment.TickCount64;
    private const double MaxDt = 1.0 / 15.0; // clamp 到最大 ~67ms

    /// <summary>
    /// 调用后返回距上次调用经过的时间（秒）。
    /// 第一次调用返回 0。
    /// </summary>
    public double Tick()
    {
        long now  = Environment.TickCount64;
        double dt = (now - _lastTick) / 1000.0;
        _lastTick = now;
        return Math.Min(dt, MaxDt);
    }
}
