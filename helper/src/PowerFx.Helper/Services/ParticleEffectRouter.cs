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
                    emitPoint = new Point(
                        caretScreenPoint.Value.X + (Random.Shared.NextDouble() - 0.5) * 16,
                        caretScreenPoint.Value.Y + (Random.Shared.NextDouble() - 0.5) * 8);
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
                // 直接使用屏幕逻辑坐标（Overlay 铺满虚拟桌面）
                var overlayPt = new Point(screenPoint.X, screenPoint.Y);
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
