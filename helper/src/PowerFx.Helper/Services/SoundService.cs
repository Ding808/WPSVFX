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
    private bool _disposed;

    private static readonly string AudioDir =
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "wt-powerfx", "audio");

    public void LoadSounds()
    {
        LoadSound("key",       System.IO.Path.Combine(AudioDir, "key.wav"));
        LoadSound("backspace", System.IO.Path.Combine(AudioDir, "backspace.wav"));
        LoadSound("delete",    System.IO.Path.Combine(AudioDir, "delete.wav"));
        LoadSound("select",    System.IO.Path.Combine(AudioDir, "select.wav"));
    }

    private void LoadSound(string key, string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Logger.Warn("SoundService", $"音频文件未找到，跳过: {path}");
            return;
        }
        try
        {
            _sounds[key] = new CachedSound(path);
            Logger.Info("SoundService", $"已加载音效: {key} ← {path}");
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
            KeyEventType.Backspace => "backspace",
            KeyEventType.Delete    => "delete",
            KeyEventType.Enter     => "key",
            KeyEventType.CtrlA     => "select",
            _                      => "key"
        };
        PlayThrottled(soundKey);
    }

    public void PlaySelect() => PlayThrottled("select");

    private void PlayThrottled(string key)
    {
        long now = Environment.TickCount64;
        long last = _lastPlayTick.GetOrAdd(key, 0L);
        if (now - last < ThrottleMs) return;

        _lastPlayTick[key] = now;

        if (!_sounds.TryGetValue(key, out var sound)) return;

        try
        {
            // 每次新建 provider（CachedSound 是只读字节缓冲，可安全复用）
            var provider = new CachedSoundSampleProvider(sound);
            var mixer    = new MixingSampleProvider(provider.WaveFormat) { ReadFully = false };
            mixer.AddMixerInput(provider);

            var output = new WaveOutEvent();
            output.Init(mixer);
            output.PlaybackStopped += (_, _) => output.Dispose();
            output.Play();
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
