using System.Windows;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 全局异常兜底：防止因未处理异常导致进程无声退出
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Logger.Error("UnhandledException", args.ExceptionObject as Exception);
        };

        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Error("DispatcherUnhandledException", args.Exception);
            args.Handled = true; // 防止崩溃
        };

        base.OnStartup(e);
    }
}
