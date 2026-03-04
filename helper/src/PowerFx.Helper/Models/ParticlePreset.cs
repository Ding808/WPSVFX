using System.Windows.Media;

namespace PowerFx.Helper.Models;

/// <summary>
/// 粒子预设参数。定义一次发射的粒子外观和物理行为。
/// </summary>
public sealed class ParticlePreset
{
    public string  Name        { get; init; } = string.Empty;
    public int     Count       { get; init; } = 8;
    public double  MinSpeed    { get; init; } = 60.0;
    public double  MaxSpeed    { get; init; } = 140.0;
    public double  MinSize     { get; init; } = 2.0;
    public double  MaxSize     { get; init; } = 5.0;
    public double  MinLifetime { get; init; } = 0.4;
    public double  MaxLifetime { get; init; } = 0.7;
    public double  Gravity     { get; init; } = 200.0;
    public bool    Upward      { get; init; } = true;
    public bool    Shrink      { get; init; } = false;
    public bool    Implode     { get; init; } = false;
    public Color[] Palette     { get; init; } = [System.Windows.Media.Colors.White];
}

/// <summary>
/// 内置粒子预设集合。
/// </summary>
public static class ParticlePresets
{
    public static readonly ParticlePreset Normal = new()
    {
        Name       = "Normal",
        Count      = 12, // 稍微减少数量以免太糊
        MinSpeed   = 30, // 降低散开距离
        MaxSpeed   = 80, // 降低散开距离
        MinSize    = 3,
        MaxSize    = 6,
        MinLifetime= 0.25,
        MaxLifetime= 0.45,
        Gravity    = 0,
        Upward     = false,
        Shrink     = true,
        Implode    = true,  // 向中心收缩
        Palette    = [Color.FromRgb(50, 150, 255), Color.FromRgb(100, 200, 255), Color.FromRgb(0, 80, 255)]
    };

    public static readonly ParticlePreset Backspace = new()
    {
        Name       = "Backspace",
        Count      = 18, // 减少太散的火花
        MinSpeed   = 50, // 聚集迸发
        MaxSpeed   = 120, // 降低最大速度，让爆发更集中
        MinSize    = 3,
        MaxSize    = 7,
        MinLifetime= 0.3,
        MaxLifetime= 0.6,
        Gravity    = 150,
        Upward     = false,  // 向四面散射
        Shrink     = true,
        Palette    = [Color.FromRgb(255, 30, 30), Color.FromRgb(255, 80, 50), Color.FromRgb(200, 0, 0)]
    };

    public static readonly ParticlePreset Delete = new()
    {
        Name       = "Delete",
        Count      = 18,
        MinSpeed   = 50,
        MaxSpeed   = 120,
        MinSize    = 3,
        MaxSize    = 7,
        MinLifetime= 0.3,
        MaxLifetime= 0.6,
        Gravity    = 150,
        Upward     = false,
        Shrink     = true,
        Palette    = [Color.FromRgb(255, 30, 30), Color.FromRgb(255, 80, 50), Color.FromRgb(200, 0, 0)]
    };

    public static readonly ParticlePreset Enter = new()
    {
        Name       = "Enter",
        Count      = 3, // 少量主闪电线条
        MinSpeed   = 50,
        MaxSpeed   = 100,
        MinSize    = 2,
        MaxSize    = 4,
        MinLifetime= 0.2,
        MaxLifetime= 0.4,
        Gravity    = 0,
        Upward     = true,
        Palette    = [Color.FromRgb(100, 200, 255), Color.FromRgb(200, 240, 255)]
    };

    public static readonly ParticlePreset Selection = new()
    {
        Name       = "Selection",
        Count      = 10,
        MinSpeed   = 30,
        MaxSpeed   = 80,
        MinSize    = 2,
        MaxSize    = 5,
        MinLifetime= 0.6,
        MaxLifetime= 1.0,
        Gravity    = 80,
        Upward     = true,
        Palette    = [Color.FromRgb(80, 180, 255), Color.FromRgb(160, 120, 255)]
    };
}
