using System.IO;

namespace PowerFx.Helper.Utils;

/// <summary>
/// 获取跨模块共用的路径常量。
/// </summary>
public static class PathUtils
{
    private const string AppName = "wt-powerfx";

    /// <summary>%APPDATA%\wt-powerfx</summary>
    public static string GetAppDataDir()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName);

    /// <summary>%APPDATA%\wt-powerfx\audio</summary>
    public static string GetAudioDir()
        => Path.Combine(GetAppDataDir(), "audio");

    /// <summary>%APPDATA%\wt-powerfx\shaders\powerfx.hlsl</summary>
    public static string GetShaderPath()
        => Path.Combine(GetAppDataDir(), "shaders", "powerfx.hlsl");

    /// <summary>获取当前 exe 所在目录。</summary>
    public static string GetExeDir()
        => AppContext.BaseDirectory;

    /// <summary>拼接 exe 目录下的相对路径。</summary>
    public static string Resolve(params string[] parts)
        => Path.Combine([GetExeDir(), ..parts]);
}
