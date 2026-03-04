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
        ParticlePreset? preset = evt.EventType switch
        {
            KeyEventType.Backspace => ParticlePresets.Backspace,
            KeyEventType.Delete    => ParticlePresets.Delete,
            KeyEventType.Enter     => ParticlePresets.Enter,
            KeyEventType.Tab       => ParticlePresets.Enter, // 同为蓝色闪电
            KeyEventType.CtrlA     => ParticlePresets.Selection,
            KeyEventType.Arrow     => null, // 方向键由 Bootstrapper 单独处理位移刀光，不放常规粒子
            _                      => ParticlePresets.Normal
        };

        if (preset == null) return;

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
                        caretScreenPoint.Value.X - _overlay.Left,
                        caretScreenPoint.Value.Y - _overlay.Top);
                        
                    if (preset.Name != "Enter")
                    {
                        emitPoint.X += (Random.Shared.NextDouble() - 0.5) * 16;
                        emitPoint.Y += (Random.Shared.NextDouble() - 0.5) * 8;
                    }
                }
                else
                {
                    emitPoint = GetRandomEmitPoint();
                }

                _overlay.Emitter.Emit(emitPoint, preset);

                // 发射光标方块
                if (caretScreenPoint.HasValue)
                {
                    // 向上微调Y轴12个像素以绝对居中对齐文字本身（进一步大幅修正下沉，使外框完美对齐字母）
                    var blockPoint = new Point(
                        caretScreenPoint.Value.X - _overlay.Left - 4,
                        caretScreenPoint.Value.Y - _overlay.Top - 12);
                        
                    // 定义大致的被占用的字符大小。
                    // 为了让方块完全包裹住光标，我们用稍宽稍微高一点的矩形
                    double charWidth = 14.0;
                    double charHeight = 26.0;
                    // 输入普通则偏蓝青色，删除则偏红
                    System.Windows.Media.Color blockColor;
                    if (evt.EventType == KeyEventType.Backspace || evt.EventType == KeyEventType.Delete)
                    {
                        blockColor = System.Windows.Media.Color.FromArgb(160, 255, 40, 40);
                        
                        // 退格删除时，实际上我们希望发光和粒子可以向刚才真正按下的前方对齐（稍微向左偏移一定距离，因为光标实际上已经缩回来了）
                        blockPoint.X -= 8.0; 
                        emitPoint.X -= 8.0;
                    }
                    else
                    {
                        blockColor = System.Windows.Media.Color.FromArgb(160, 50, 220, 255);
                    }

                    _overlay.Emitter.EmitStaticBlock(blockPoint, charWidth, charHeight, blockColor, 0.4);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("ParticleEffectRouter", $"发射粒子失败: {ex.Message}");
            }
        });
    }

    public void EmitSlash(Point oldCaret, Point newCaret)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                var start = new Point(oldCaret.X - _overlay.Left, oldCaret.Y - _overlay.Top);
                var end = new Point(newCaret.X - _overlay.Left, newCaret.Y - _overlay.Top);
                _overlay.Emitter.EmitSlash(start, end);
            }
            catch (Exception ex)
            {
                Logger.Warn("ParticleEffectRouter", $"发射刀光失败: {ex.Message}");
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
