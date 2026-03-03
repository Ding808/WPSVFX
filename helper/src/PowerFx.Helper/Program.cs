using System.Threading;
using System.Windows;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper;

/// <summary>
/// 应用程序入口点。
/// 使用 Mutex 保证单实例运行，然后启动 WPF Application。
/// </summary>
internal static class Program
{
    private const string MutexName = "Global\\PowerFx.Helper.SingleInstance";

    [STAThread]
    private static int Main(string[] args)
    {
        Logger.Init();
        Logger.Info("Program", "PowerFx.Helper 启动中...");

        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            Logger.Warn("Program", "已有实例在运行，退出。");
            MessageBox.Show(
                "PowerFx.Helper 已在运行。",
                "wt-powerfx",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return 1;
        }

        try
        {
            var app = new App();
            var bootstrapper = new MainBootstrapper();
            app.Startup += (_, _) => bootstrapper.Start(app);
            app.Exit += (_, _) => bootstrapper.Stop();

            return app.Run();
        }
        catch (Exception ex)
        {
            Logger.Error("Program", ex);
            return -1;
        }
    }
}
