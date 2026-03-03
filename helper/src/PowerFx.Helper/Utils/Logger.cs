using System.IO;
using System.Text;

namespace PowerFx.Helper.Utils;

/// <summary>
/// 极简双写日志（文件 + Console）。
/// 初始化后写入 %APPDATA%\wt-powerfx\helper.log。
/// 线程安全（lock 保护写入顺序）。
/// </summary>
public static class Logger
{
    private static StreamWriter? _writer;
    private static readonly object _lock = new();

    public static void Init()
    {
        var logDir  = PathUtils.GetAppDataDir();
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "helper.log");

        try
        {
            _writer = new StreamWriter(logPath, append: true, encoding: Encoding.UTF8)
            {
                AutoFlush = true
            };
            Info("Logger", $"日志已初始化 → {logPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Logger] 无法打开日志文件: {ex.Message}");
        }
    }

    public static bool DebugEnabled { get; set; } = true;   // 诊断期间默认开启

    public static void Info(string module, string msg)  => Write("INFO ", module, msg);
    public static void Warn(string module, string msg)  => Write("WARN ", module, msg);
    public static void Error(string module, string msg) => Write("ERROR", module, msg);
    public static void Debug(string module, string msg) { if (DebugEnabled) Write("DEBUG", module, msg); }

    public static void Error(string module, Exception? ex)
    {
        if (ex == null) return;
        Error(module, $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
    }

    private static void Write(string level, string module, string msg)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{module}] {msg}";
        lock (_lock)
        {
            Console.WriteLine(line);
            try { _writer?.WriteLine(line); }
            catch { /* 日志失败不能崩进程 */ }
        }
    }
}
