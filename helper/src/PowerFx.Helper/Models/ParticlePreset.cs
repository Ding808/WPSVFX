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
        Count      = 6,
        MinSpeed   = 50,
        MaxSpeed   = 120,
        MinSize    = 2,
        MaxSize    = 4,
        MinLifetime= 0.35,
        MaxLifetime= 0.55,
        Gravity    = 150,
        Upward     = true,
        Palette    = [Color.FromRgb(200, 200, 255), Color.FromRgb(180, 230, 255)]
    };

    public static readonly ParticlePreset Backspace = new()
    {
        Name       = "Backspace",
        Count      = 14,
        MinSpeed   = 80,
        MaxSpeed   = 200,
        MinSize    = 3,
        MaxSize    = 7,
        MinLifetime= 0.45,
        MaxLifetime= 0.7,
        Gravity    = 180,
        Upward     = false,  // 向四面散射
        Palette    = [Color.FromRgb(255, 100, 80), Color.FromRgb(255, 180, 60)]
    };

    public static readonly ParticlePreset Delete = new()
    {
        Name       = "Delete",
        Count      = 12,
        MinSpeed   = 70,
        MaxSpeed   = 180,
        MinSize    = 3,
        MaxSize    = 6,
        MinLifetime= 0.4,
        MaxLifetime= 0.65,
        Gravity    = 160,
        Upward     = false,
        Palette    = [Color.FromRgb(255, 70, 70), Color.FromRgb(220, 80, 255)]
    };

    public static readonly ParticlePreset Enter = new()
    {
        Name       = "Enter",
        Count      = 20,
        MinSpeed   = 100,
        MaxSpeed   = 260,
        MinSize    = 3,
        MaxSize    = 8,
        MinLifetime= 0.5,
        MaxLifetime= 0.9,
        Gravity    = 220,
        Upward     = true,
        Palette    = [Color.FromRgb(120, 255, 180), Color.FromRgb(60, 200, 255), Color.FromRgb(255, 255, 100)]
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
