using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MindCanvas.Core.Commands;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    private enum NodeDropZone
    {
        Child,
        Before,
        After
    }

    private sealed record NodeDropPlan(Guid ParentId, int? Index, Guid TargetId, NodeDropZone Zone);

    private bool _nodeDragInitialized;
    private uint? _nodeDragPointerId;
    private Guid? _nodeDragSourceId;
    private Point _nodeDragStart;
    private bool _nodeDragActive;
    private Border? _nodeDragGhost;
    private Border? _nodeDropHighlight;
    private Border? _nodeDragHint;
    private NodeDropPlan? _nodeDropPlan;

    public void InitializeNodeReparentDrag()
    {
        if (_nodeDragInitialized)
            return;

        _nodeDragInitialized = true;
        MapCanvas.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(NodeDrag_PointerPressed), handledEventsToo: true);
        MapCanvas.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(NodeDrag_PointerMoved), handledEventsToo: true);
        MapCanvas.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(NodeDrag_PointerReleased), handledEventsToo: true);
        MapCanvas.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(NodeDrag_PointerCanceled), handledEventsToo: true);
        MapCanvas.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(NodeDrag_PointerCaptureLost), handledEventsToo: true);
    }

    private void NodeDrag_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_document is null || _history is null || e.Pointer.PointerDeviceType is not Windows.Devices.Input.PointerDeviceType.Mouse)
            return;

        var point = e.GetCurrentPoint(MapCanvas);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        var nodeId = FindNodeAtPoint(point.Position);
        if (nodeId is null || nodeId == _document.RootNodeId)
            return;

        _nodeDragPointerId = e.Pointer.PointerId;
        _nodeDragSourceId = nodeId;
        _nodeDragStart = point.Position;
        _nodeDragActive = false;
        _nodeDropPlan = null;
        MapCanvas.CapturePointer(e.Pointer);
    }

    private void NodeDrag_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_document is null || _nodeDragPointerId != e.Pointer.PointerId || _nodeDragSourceId is not Guid sourceId)
            return;

        var point = e.GetCurrentPoint(MapCanvas);
        if (!point.Properties.IsLeftButtonPressed)
        {
            CancelNodeDrag(refresh: false);
            return;
        }

        if (!_nodeDragActive)
        {
            var dx = point.Position.X - _nodeDragStart.X;
            var dy = point.Position.Y - _nodeDragStart.Y;
            if (Math.Sqrt(dx * dx + dy * dy) < 7)
                return;

            _nodeDragActive = true;
            CreateNodeDragVisuals(sourceId);
        }

        UpdateNodeDragVisuals(point.Position, sourceId);
        e.Handled = true;
    }

    private void NodeDrag_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_nodeDragPointerId != e.Pointer.PointerId)
            return;

        MapCanvas.ReleasePointerCapture(e.Pointer);
        if (!_nodeDragActive || _document is null || _history is null || _nodeDragSourceId is not Guid sourceId || _nodeDropPlan is not { } plan)
        {
            CancelNodeDrag(refresh: false);
            return;
        }

        try
        {
            var copy = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            if (copy)
            {
                var template = NodeSubtreeTemplate.Capture(_document, sourceId);
                var command = new InsertSubtreeCommand(_document, plan.ParentId, template, plan.Index);
                _history.Execute(command);
                _selectedNodeId = command.CreatedRootId ?? sourceId;
            }
            else
            {
                if (IsNoOpDrop(sourceId, plan))
                {
                    CancelNodeDrag(refresh: false);
                    return;
                }
                _history.Execute(new MoveNodeCommand(_document, sourceId, plan.ParentId, plan.Index));
                _selectedNodeId = sourceId;
            }

            CancelNodeDrag(refresh: false);
            NotifyMutation();
        }
        catch (InvalidOperationException)
        {
            CancelNodeDrag(refresh: true);
        }

        e.Handled = true;
    }

    private void NodeDrag_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_nodeDragPointerId == e.Pointer.PointerId)
            CancelNodeDrag(refresh: true);
    }

    private void NodeDrag_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (_nodeDragPointerId == e.Pointer.PointerId && _nodeDragActive)
            CancelNodeDrag(refresh: true);
    }

    private Guid? FindNodeAtPoint(Point point)
    {
        foreach (var border in MapCanvas.Children.OfType<Border>().Reverse())
        {
            if (border.Tag is not Guid nodeId)
                continue;
            var left = Canvas.GetLeft(border);
            var top = Canvas.GetTop(border);
            if (double.IsNaN(left) || double.IsNaN(top))
                continue;
            var width = border.ActualWidth > 0 ? border.ActualWidth : border.Width;
            var height = border.ActualHeight > 0 ? border.ActualHeight : border.Height;
            if (point.X >= left && point.X <= left + width && point.Y >= top && point.Y <= top + height)
                return nodeId;
        }
        return null;
    }

    private NodeDropPlan? BuildDropPlan(Guid sourceId, Point point)
    {
        if (_document is null)
            return null;

        var targetId = FindNodeAtPoint(point);
        if (targetId is not Guid target || target == sourceId || !_document.Nodes.ContainsKey(target))
            return null;

        var targetElement = MapCanvas.Children.OfType<Border>().FirstOrDefault(border => border.Tag is Guid id && id == target);
        if (targetElement is null)
            return null;

        var top = Canvas.GetTop(targetElement);
        var height = targetElement.ActualHeight > 0 ? targetElement.ActualHeight : targetElement.Height;
        var relativeY = height <= 0 ? 0.5 : (point.Y - top) / height;
        var zone = relativeY < 0.25 ? NodeDropZone.Before : relativeY > 0.75 ? NodeDropZone.After : NodeDropZone.Child;

        if (zone == NodeDropZone.Child)
        {
            if (!CanMoveToParent(sourceId, target))
                return null;
            return new NodeDropPlan(target, null, target, zone);
        }

        var targetNode = _document.GetNode(target);
        if (targetNode.ParentId is not Guid parentId || !CanMoveToParent(sourceId, parentId))
            return null;

        var siblings = _document.GetNode(parentId).ChildrenIds;
        var targetIndex = siblings.IndexOf(target);
        if (targetIndex < 0)
            return null;

        var source = _document.GetNode(sourceId);
        var sourceIndex = source.ParentId == parentId ? siblings.IndexOf(sourceId) : -1;
        var adjustedTargetIndex = sourceIndex >= 0 && sourceIndex < targetIndex ? targetIndex - 1 : targetIndex;
        var insertionIndex = zone == NodeDropZone.Before ? adjustedTargetIndex : adjustedTargetIndex + 1;
        return new NodeDropPlan(parentId, insertionIndex, target, zone);
    }

    private bool CanMoveToParent(Guid sourceId, Guid parentId)
    {
        if (_document is null || sourceId == parentId)
            return false;

        var current = _document.GetNode(parentId);
        while (current.ParentId is Guid ancestorId)
        {
            if (ancestorId == sourceId)
                return false;
            current = _document.GetNode(ancestorId);
        }
        return true;
    }

    private bool IsNoOpDrop(Guid sourceId, NodeDropPlan plan)
    {
        if (_document is null)
            return true;

        var node = _document.GetNode(sourceId);
        if (node.ParentId != plan.ParentId)
            return false;
        if (plan.Index is null)
            return false;

        var siblings = _document.GetNode(plan.ParentId).ChildrenIds;
        var current = siblings.IndexOf(sourceId);
        return current == plan.Index || current + 1 == plan.Index;
    }

    private void CreateNodeDragVisuals(Guid sourceId)
    {
        if (_document is null)
            return;

        _nodeDragGhost = new Border
        {
            Width = 166,
            Height = 42,
            CornerRadius = new CornerRadius(7),
            Background = ResourceBrush("V4ContextActionsBackgroundBrush", Colors.White),
            BorderBrush = ResourceBrush("V4ControlSelectedStrokeBrush", ColorHelper.FromArgb(255, 96, 166, 230)),
            BorderThickness = new Thickness(1.5),
            Opacity = 0.84,
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = _document.GetNode(sourceId).Title,
                Margin = new Thickness(14, 0, 14, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        };
        Canvas.SetZIndex(_nodeDragGhost, 1000);
        MapCanvas.Children.Add(_nodeDragGhost);

        _nodeDropHighlight = new Border
        {
            CornerRadius = new CornerRadius(9),
            Background = new SolidColorBrush(ColorHelper.FromArgb(22, 8, 107, 194)),
            BorderBrush = ResourceBrush("V4ControlSelectedStrokeBrush", ColorHelper.FromArgb(255, 96, 166, 230)),
            BorderThickness = new Thickness(2),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        Canvas.SetZIndex(_nodeDropHighlight, 999);
        MapCanvas.Children.Add(_nodeDropHighlight);

        _nodeDragHint = new Border
        {
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            CornerRadius = new CornerRadius(17),
            Background = ResourceBrush("V4ContextActionsBackgroundBrush", Colors.White),
            BorderBrush = ResourceBrush("V4CardStrokeBrush", ColorHelper.FromArgb(255, 220, 226, 232)),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = T("Drag to a node to reparent · Alt copies · Esc cancels", "拖到节点中心设为子节点 · Alt 复制 · Esc 取消", "ノード中央へドラッグで子ノード化 · Alt でコピー · Esc で取消"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.72
            }
        };
        Canvas.SetLeft(_nodeDragHint, Math.Max(16, MapScrollViewer.HorizontalOffset / Math.Max(0.01, MapScrollViewer.ZoomFactor) + 20));
        Canvas.SetTop(_nodeDragHint, Math.Max(16, MapScrollViewer.VerticalOffset / Math.Max(0.01, MapScrollViewer.ZoomFactor) + 18));
        Canvas.SetZIndex(_nodeDragHint, 1001);
        MapCanvas.Children.Add(_nodeDragHint);
    }

    private void UpdateNodeDragVisuals(Point point, Guid sourceId)
    {
        if (_nodeDragGhost is not null)
        {
            Canvas.SetLeft(_nodeDragGhost, point.X + 14);
            Canvas.SetTop(_nodeDragGhost, point.Y + 14);
        }

        _nodeDropPlan = BuildDropPlan(sourceId, point);
        if (_nodeDropPlan is not { } plan || _nodeDropHighlight is null)
        {
            if (_nodeDropHighlight is not null)
                _nodeDropHighlight.Visibility = Visibility.Collapsed;
            MapStatusText.Text = T("Move node · no valid drop target", "移动节点 · 当前没有有效放置目标", "ノード移動 · 有効なドロップ先がありません");
            return;
        }

        var target = MapCanvas.Children.OfType<Border>().FirstOrDefault(border => border.Tag is Guid id && id == plan.TargetId);
        if (target is null)
            return;

        var left = Canvas.GetLeft(target);
        var top = Canvas.GetTop(target);
        var width = target.ActualWidth > 0 ? target.ActualWidth : target.Width;
        var height = target.ActualHeight > 0 ? target.ActualHeight : target.Height;
        _nodeDropHighlight.Visibility = Visibility.Visible;

        if (plan.Zone == NodeDropZone.Child)
        {
            Canvas.SetLeft(_nodeDropHighlight, left - 8);
            Canvas.SetTop(_nodeDropHighlight, top - 8);
            _nodeDropHighlight.Width = width + 16;
            _nodeDropHighlight.Height = height + 16;
            _nodeDropHighlight.CornerRadius = new CornerRadius(9);
            MapStatusText.Text = T("Release to make this node a child", "松开：设为该节点的子节点", "離すとこのノードの子になります");
        }
        else
        {
            Canvas.SetLeft(_nodeDropHighlight, left - 6);
            Canvas.SetTop(_nodeDropHighlight, plan.Zone == NodeDropZone.Before ? top - 5 : top + height + 1);
            _nodeDropHighlight.Width = width + 12;
            _nodeDropHighlight.Height = 4;
            _nodeDropHighlight.CornerRadius = new CornerRadius(2);
            MapStatusText.Text = plan.Zone == NodeDropZone.Before
                ? T("Release to insert before", "松开：插入到该节点之前", "離すとこのノードの前に挿入")
                : T("Release to insert after", "松开：插入到该节点之后", "離すとこのノードの後に挿入");
        }
    }

    private void CancelNodeDrag(bool refresh)
    {
        _nodeDragPointerId = null;
        _nodeDragSourceId = null;
        _nodeDropPlan = null;
        _nodeDragActive = false;

        if (_nodeDragGhost is not null)
            MapCanvas.Children.Remove(_nodeDragGhost);
        if (_nodeDropHighlight is not null)
            MapCanvas.Children.Remove(_nodeDropHighlight);
        if (_nodeDragHint is not null)
            MapCanvas.Children.Remove(_nodeDragHint);

        _nodeDragGhost = null;
        _nodeDropHighlight = null;
        _nodeDragHint = null;

        if (refresh)
            Refresh();
    }
}
