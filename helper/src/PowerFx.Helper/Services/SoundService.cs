using System.Collections.Concurrent;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PowerFx.Helper.Models;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 使用 NAudio 播放音效。
/// 内置节流：同一音效在 <see cref="ThrottleMs"/> 毫秒内只播放一次，防止卡顿。
/// </summary>
public sealed class SoundService : IDisposable
{
    public int ThrottleMs { get; set; } = 80;

    private readonly Dictionary<string, CachedSound> _sounds = new();
    private readonly ConcurrentDictionary<string, long> _lastPlayTick = new();
    private WaveOutEvent? _outputDevice;
    private MixingSampleProvider? _mixer;
    private WaveFormat? _targetFormat;
    private bool _disposed;

    // 优先从环境变量配置的目录或开发者目录载入，后备到本地 AppData
    private static readonly string AppDataAudioDir =
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "wt-powerfx", "audio");

    private static string GetAudioPath(string filename)
    {
        // Debug/本地 环境下自动寻路到项目里的 assets/audio，方便测试
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            string testPath = System.IO.Path.Combine(dir, "assets", "audio", filename);
            if (System.IO.File.Exists(testPath)) return testPath;
            dir = System.IO.Path.Combine(dir, "..");
        }

        return System.IO.Path.Combine(AppDataAudioDir, filename);
    }

    public void LoadSounds()
    {
        try
        {
            // 初始化全局音频混合器与输出设备，极大降低延迟和CPU占用
            _targetFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            _mixer = new MixingSampleProvider(_targetFormat) { ReadFully = true };
            _outputDevice = new WaveOutEvent { DesiredLatency = 100 }; // 降低延迟，提高响应速度
            _outputDevice.Init(_mixer);
            _outputDevice.Play(); // 保持常驻播放状态（输出静音直到有音频混入）

            LoadSound("input",     GetAudioPath("whoosh.mp3"));
            LoadSound("delete",    GetAudioPath("CinematicBoom.mp3"));
            LoadSound("enter",     GetAudioPath("Lightning.mp3"));
            LoadSound("select",    GetAudioPath("click.mp3"));
        }
        catch (Exception ex)
        {
            Logger.Warn("SoundService", $"初始化全局音频设备失败: {ex.Message}");
        }
    }

    private void LoadSound(string key, string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Logger.Warn("SoundService", $"音频文件未找到，跳过: {path}");
            return;
        }
        if (_targetFormat == null) return;

        try
        {
            _sounds[key] = new CachedSound(path, _targetFormat);
            Logger.Info("SoundService", $"已加载音效并重采样: {key} ← {path}");
        }
        catch (Exception ex)
        {
            Logger.Warn("SoundService", $"加载音效失败 {key}: {ex.Message}");
        }
    }

    public void PlayForEvent(KeyEffectEvent evt)
    {
        var soundKey = evt.EventType switch
        {
            KeyEventType.Backspace => "delete",
            KeyEventType.Delete    => "delete",
            KeyEventType.Enter     => "enter",
            KeyEventType.Tab       => "enter",
            KeyEventType.CtrlA     => "select",
            KeyEventType.Arrow     => null, // 方向键无需音效，保持安静
            _                      => "input"
        };
        
        if (soundKey != null)
        {
            PlayThrottled(soundKey);
        }
    }

    public void PlaySelect() => PlayThrottled("select");

    private void PlayThrottled(string key)
    {
        if (_mixer == null) return;

        long now = Environment.TickCount64;
        long last = _lastPlayTick.GetOrAdd(key, 0L);
        if (now - last < ThrottleMs) return;

        _lastPlayTick[key] = now;

        if (!_sounds.TryGetValue(key, out var sound)) return;

        try
        {
            // 对于并发低延迟播放，只需将提供者混入处于一直运行状态的_mixer之中即可
            var provider = new CachedSoundSampleProvider(sound);
            _mixer.AddMixerInput(provider);
        }
        catch (Exception ex)
        {
            Logger.Warn("SoundService", $"播放音效异常 {key}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_outputDevice != null)
        {
            try
            {
                _outputDevice.Stop();
                _outputDevice.Dispose();
            }
            catch { }
            _outputDevice = null;
        }

        foreach (var s in _sounds.Values) s.Dispose();
        _sounds.Clear();
    }
}

// ── NAudio 辅助类（CachedSound）──────────────────────────────────────────────

/// <summary>
/// 将 WAV 文件预缓存为 float[] 内存，避免每次播放都读磁盘。
/// </summary>
internal sealed class CachedSound : IDisposable
{
    public float[] AudioData { get; }
    public WaveFormat WaveFormat { get; }

    public CachedSound(string path, WaveFormat targetFormat)
    {
        using var reader = new AudioFileReader(path);
        
        ISampleProvider provider = reader;
        
        // 如果文件本身的采样率/声道数与全局混合器不一样，则在这里重采样
        if (reader.WaveFormat.SampleRate != targetFormat.SampleRate || reader.WaveFormat.Channels != targetFormat.Channels)
        {
            provider = new MediaFoundationResampler(reader, targetFormat).ToSampleProvider();
        }

        WaveFormat = targetFormat;
        var buf   = new List<float>();
        var block = new float[WaveFormat.SampleRate * WaveFormat.Channels]; // 读取时使用新的格式缓冲区
        int read;
        while ((read = provider.Read(block, 0, block.Length)) > 0)
            buf.AddRange(block.Take(read));
        AudioData = buf.ToArray();
    }

    public CachedSound(string path)
    {
        using var reader  = new AudioFileReader(path);
        WaveFormat = reader.WaveFormat;
        var buf   = new List<float>();
        var block = new float[reader.WaveFormat.SampleRate];
        int read;
        while ((read = reader.Read(block, 0, block.Length)) > 0)
            buf.AddRange(block.Take(read));
        AudioData = buf.ToArray();
    }

    public void Dispose() { /* AudioData is managed memory */ }
}

internal sealed class CachedSoundSampleProvider : ISampleProvider
{
    private readonly CachedSound _sound;
    private int _position;

    public WaveFormat WaveFormat => _sound.WaveFormat;

    public CachedSoundSampleProvider(CachedSound sound) => _sound = sound;

    public int Read(float[] buffer, int offset, int count)
    {
        int available = _sound.AudioData.Length - _position;
        int toRead    = Math.Min(available, count);
        Array.Copy(_sound.AudioData, _position, buffer, offset, toRead);
        _position += toRead;
        return toRead;
    }
}
