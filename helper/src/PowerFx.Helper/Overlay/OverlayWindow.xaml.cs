using System.Windows;
using System.Windows.Interop;
using PowerFx.Helper.Native;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Overlay;

/// <summary>
/// 透明置顶点击穿透 WPF 窗口。
/// WndProc 里拦截 WM_MOUSEACTIVATE / WM_NCACTIVATE，确保 Overlay 永不抢 WT 焦点。
/// </summary>
public partial class OverlayWindow : Window
{
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int WM_NCACTIVATE    = 0x0086;
    private const int WM_ACTIVATE      = 0x0006;
    private const int MA_NOACTIVATE    = 3;

    public ParticleEmitter Emitter    { get; }
    private OverlayRenderer? _renderer;

    /// <summary>hwnd 就绪后触发，供 ForegroundTerminalService 排除自身。</summary>
    public event Action<IntPtr>? HwndReady;

    /// <summary>当前 Overlay 窗口句柄（Show 前为 Zero）。</summary>
    public IntPtr OverlayHwnd { get; private set; }

    public OverlayWindow()
    {
        InitializeComponent();
        Emitter = new ParticleEmitter();

        SourceInitialized += OnSourceInitialized;
        Loaded             += OnLoaded;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        OverlayHwnd = new WindowInteropHelper(this).Handle;

        WindowInterop.SetOverlayStyle(OverlayHwnd);

        // 拦截激活消息，防止抢夺 WT 焦点
        HwndSource.FromHwnd(OverlayHwnd)?.AddHook(WndProc);

        // 通知外部 hwnd 已就绪
        HwndReady?.Invoke(OverlayHwnd);

        Logger.Info("OverlayWindow", $"overlay 窗口初始化完成 hwnd=0x{OverlayHwnd:X}");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_MOUSEACTIVATE:
                handled = true;
                return (IntPtr)MA_NOACTIVATE;

            case WM_NCACTIVATE:
                handled = true;
                return IntPtr.Zero;

            case WM_ACTIVATE:
                if (wParam != IntPtr.Zero)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
                break;
        }
        return IntPtr.Zero;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _renderer = new OverlayRenderer(ParticleCanvas, Emitter);
        _renderer.Start();
        Logger.Info("OverlayWindow", "渲染器已启动");
    }

    protected override void OnClosed(EventArgs e)
    {
        _renderer?.Stop();
        base.OnClosed(e);
    }
}
