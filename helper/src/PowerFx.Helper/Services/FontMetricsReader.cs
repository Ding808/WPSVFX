using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 从 Windows Terminal settings.json 读取当前活跃 profile 的字体配置和内容 padding，
/// 供 <see cref="ConsoleCursorService"/> 精确换算屏幕坐标使用。
///
/// 结果被缓存5秒，避免频繁磁盘 IO。
/// </summary>
public sealed class FontMetricsReader
{
    public record Metrics(
        int FontSize,       // pt
        int ContentPadding, // px（取 padding 四个方向中的 top）
        int TabBarHeight);  // px

    private static readonly string[] SettingsCandidates =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Packages", "Microsoft.WindowsTerminal_8wekyb3d8bbwe", "LocalState", "settings.json"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Packages", "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe", "LocalState", "settings.json"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows Terminal", "settings.json")
    ];

    private Metrics? _cached;
    private long     _cachedAt;
    private const long CacheMs = 5000;

    /// <summary>
    /// 获取字体和布局指标。解析失败时返回内置默认值，不抛出异常。
    /// </summary>
    public Metrics Read()
    {
        long now = Environment.TickCount64;
        if (_cached != null && now - _cachedAt < CacheMs)
            return _cached;

        _cached   = ParseSettings() ?? new Metrics(FontSize: 12, ContentPadding: 8, TabBarHeight: 36);
        _cachedAt = now;
        return _cached;
    }

    private static Metrics? ParseSettings()
    {
        string? path = SettingsCandidates.FirstOrDefault(File.Exists);
        if (path is null) return null;

        try
        {
            var text = File.ReadAllText(path);
            // 用 System.Text.Json 解析（settings.json 包含注释，需先剥离）
            // 简单剥离行注释 // 和 /* */ 块注释
            text = StripJsoncComments(text);

            var root = JsonNode.Parse(text);
            if (root is null) return null;

            // fontSize：优先 profiles.defaults.fontSize，其次根级
            int fontSize = 12;
            var defaults = root["profiles"]?["defaults"];
            if (defaults?["fontSize"] is JsonValue fv && fv.TryGetValue(out int fvInt))
                fontSize = fvInt;

            // padding：格式 "8, 8, 8, 8" 或 "8"
            int padding = 8;
            if (defaults?["padding"] is JsonValue pv && pv.TryGetValue(out string? padStr) && padStr is not null)
                padding = ParseFirstPaddingValue(padStr);

            // tab bar high：WT 默认 36px（showTabsInTitlebar=false 时为0，简化起见固定36）
            int tabBar = 36;

            return new Metrics(fontSize, padding, tabBar);
        }
        catch (Exception ex)
        {
            Logger.Warn("FontMetricsReader", $"读取 settings.json 失败: {ex.Message}");
            return null;
        }
    }

    private static int ParseFirstPaddingValue(string padStr)
    {
        // "8, 8, 8, 8" → 8 ;  "16" → 16
        var parts = padStr.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out int val))
            return val;
        return 8;
    }

    /// <summary>极简 JSONC 注释剥离（行注释 + 块注释）。</summary>
    private static string StripJsoncComments(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        int i = 0;
        bool inString = false;

        while (i < text.Length)
        {
            if (inString)
            {
                if (text[i] == '\\' && i + 1 < text.Length) { sb.Append(text[i]); sb.Append(text[i + 1]); i += 2; continue; }
                if (text[i] == '"') inString = false;
                sb.Append(text[i++]);
                continue;
            }

            if (text[i] == '"') { inString = true; sb.Append(text[i++]); continue; }

            // 行注释
            if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '/')
            {
                while (i < text.Length && text[i] != '\n') i++;
                continue;
            }

            // 块注释
            if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++;
                i += 2;
                continue;
            }

            sb.Append(text[i++]);
        }
        return sb.ToString();
    }
}
