using System.Drawing;
using PowerFx.Helper.Utils;

namespace PowerFx.Helper.Services;

/// <summary>
/// 通过鼠标低级钩子检测"左键拖动选取"行为（近似实现）。
/// 当检测到拖动开始时，触发 <see cref="SelectionStarted"/> 事件。
///
/// 近似实现说明：无法精确得知选区字符坐标，
/// 以鼠标坐标作为粒子发射的近似位置。
/// </summary>
public sealed class SelectionTrailService
{
    public event Action<Point>? SelectionStarted;
    public event Action<Point>? SelectionMoved;
    public event Action? SelectionEnded;

    private bool _isDragging;
    private Point _dragStart;
    private const int DragThreshold = 5; // px

    public void OnMouseButtonDown(Point pt)
    {
        _isDragging = false;
        _dragStart  = pt;
    }

    public void OnMouseMove(Point pt)
    {
        if (_isDragging)
        {
            SelectionMoved?.Invoke(pt);
            return;
        }

        // 判断是否已超过拖动阈值
        if (Math.Abs(pt.X - _dragStart.X) > DragThreshold ||
            Math.Abs(pt.Y - _dragStart.Y) > DragThreshold)
        {
            // 需要确认左键仍然按下（低级钩子无法直接判断状态，使用 GetKeyState）
            var state = PowerFx.Helper.Native.Win32.GetKeyState(0x01); // VK_LBUTTON
            if ((state & 0x8000) != 0)
            {
                _isDragging = true;
                SelectionStarted?.Invoke(_dragStart);
                Logger.Info("SelectionTrailService", $"拖选开始 @ ({_dragStart.X},{_dragStart.Y})");
            }
        }
    }

    public void OnMouseButtonUp(Point pt)
    {
        if (_isDragging)
        {
            _isDragging = false;
            SelectionEnded?.Invoke();
        }
    }
}
