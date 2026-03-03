using System.Windows;
using PowerFx.Helper.Models;
using PowerFx.Helper.Overlay;
using PowerFx.Helper.Utils;
using WinPoint = System.Drawing.Point;

namespace PowerFx.Helper.Services;

/// <summary>
/// 根据按键事件类型，选择合适的粒子预设并调用 <see cref="ParticleEmitter"/> 发射粒子。
/// 所有粒子都在 overlay 窗口中央附近的随机位置生成（近似实现）。
/// </summary>
public sealed class ParticleEffectRouter
{
    private readonly OverlayWindow _overlay;

    public ParticleEffectRouter(OverlayWindow overlay)
    {
        _overlay = overlay;
    }

    /// <summary>
    /// 根据按键事件发射粒子。
    /// <paramref name="caretScreenPoint"/> 为 UIAutomation 获取的光标屏幕坐标（可为 null，降级为随机位置）。
    /// </summary>
    public void Route(KeyEffectEvent evt, Point? caretScreenPoint = null)
    {
        var preset = evt.EventType switch
        {
            KeyEventType.Backspace => ParticlePresets.Backspace,
            KeyEventType.Delete    => ParticlePresets.Delete,
            KeyEventType.Enter     => ParticlePresets.Enter,
            KeyEventType.CtrlA     => ParticlePresets.Selection,
            _                      => ParticlePresets.Normal
        };

        Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                Point emitPoint;
                if (caretScreenPoint.HasValue)
                {
                    // Overlay 铺满虚拟桌面，屏幕逻辑坐标 == Overlay 本地坐标，直接用
                    // 需要减去 Overlay 的 Left/Top 获取 Canvas 本地坐标
                    emitPoint = new Point(
                        caretScreenPoint.Value.X - _overlay.Left + (Random.Shared.NextDouble() - 0.5) * 16,
                        caretScreenPoint.Value.Y - _overlay.Top + (Random.Shared.NextDouble() - 0.5) * 8);
                }
                else
                {
                    emitPoint = GetRandomEmitPoint();
                }

                _overlay.Emitter.Emit(emitPoint, preset);
            }
            catch (Exception ex)
            {
                Logger.Warn("ParticleEffectRouter", $"发射粒子失败: {ex.Message}");
            }
        });
    }

    public void EmitSelectionStart(WinPoint screenPoint)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                // WinPoint 也是屏幕物理坐标，将其转换为 WPF 逻辑坐标。
                // 偏右下角是因为物理像素直接当逻辑像素用，导致超出实际逻辑大小（相当于放大）。
                var src = PresentationSource.FromVisual(Application.Current.MainWindow);
                double dpiX = 1.0, dpiY = 1.0;
                if (src?.CompositionTarget != null)
                {
                    dpiX = src.CompositionTarget.TransformFromDevice.M11;
                    dpiY = src.CompositionTarget.TransformFromDevice.M22;
                }

                // 计算逻辑坐标
                double logicalX = screenPoint.X * dpiX;
                double logicalY = screenPoint.Y * dpiY;

                // 直接使用逻辑坐标减去 Overlay 偏移
                var overlayPt = new Point(logicalX - _overlay.Left, logicalY - _overlay.Top);
                _overlay.Emitter.Emit(overlayPt, ParticlePresets.Selection);
            }
            catch (Exception ex)
            {
                Logger.Warn("ParticleEffectRouter", $"发射选区粒子失败: {ex.Message}");
            }
        });
    }

    private System.Windows.Point GetRandomEmitPoint()
    {
        var rng = Random.Shared;
        double x = _overlay.ActualWidth  * (0.3 + rng.NextDouble() * 0.4);
        double y = _overlay.ActualHeight * (0.4 + rng.NextDouble() * 0.2);
        return new System.Windows.Point(x, y);
    }
}
