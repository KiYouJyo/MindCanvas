using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MindCanvas.Layout;
using MindCanvas.Layout.Geometry;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    private readonly ViewportLayoutFilter _viewportLayoutFilter = new(virtualizationThreshold: 180, overscan: 220);
    private readonly DispatcherTimer _viewportRenderTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private bool _viewportVirtualizationInitialized;
    private bool _viewportRenderPending;
    private bool _viewportRenderRunning;
    private bool _viewportVirtualized;
    private bool _viewportVirtualizationSuspended;

    public void InitializeViewportVirtualization()
    {
        if (_viewportVirtualizationInitialized)
            return;

        _viewportVirtualizationInitialized = true;
        _viewportRenderTimer.Tick += ViewportRenderTimer_Tick;
        MapScrollViewer.ViewChanged += ViewportVirtualization_ViewChanged;
        MapScrollViewer.SizeChanged += ViewportVirtualization_SizeChanged;
        SelectionChanged += ViewportVirtualization_SelectionChanged;
        DocumentChanged += ViewportVirtualization_DocumentChanged;
        Loaded += ViewportVirtualization_Loaded;
        ScheduleViewportRender(immediate: false);
    }

    private void ViewportVirtualization_Loaded(object sender, RoutedEventArgs e)
        => ScheduleViewportRender(immediate: false);

    private void ViewportVirtualization_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_nodeDragActive || _viewportVirtualizationSuspended)
            return;
        ScheduleViewportRender(immediate: !e.IsIntermediate);
    }

    private void ViewportVirtualization_SizeChanged(object sender, SizeChangedEventArgs e)
        => ScheduleViewportRender(immediate: false);

    private void ViewportVirtualization_SelectionChanged(object? sender, EventArgs e)
        => ScheduleViewportRender(immediate: true);

    private void ViewportVirtualization_DocumentChanged(object? sender, EventArgs e)
        => ScheduleViewportRender(immediate: true);

    private void ScheduleViewportRender(bool immediate)
    {
        if (!_viewportVirtualizationInitialized || _viewportVirtualizationSuspended || _document is null || MapHost.Visibility != Visibility.Visible)
            return;

        if (_document.EnumerateVisibleDepthFirst().Take(_viewportLayoutFilter.VirtualizationThreshold + 1).Count() <= _viewportLayoutFilter.VirtualizationThreshold)
        {
            _viewportVirtualized = false;
            _viewportRenderTimer.Stop();
            _viewportRenderPending = false;
            return;
        }

        _viewportRenderPending = true;
        _viewportRenderTimer.Stop();
        if (immediate)
        {
            ApplyViewportVirtualization();
            return;
        }
        _viewportRenderTimer.Start();
    }

    private void ViewportRenderTimer_Tick(object? sender, object e)
    {
        _viewportRenderTimer.Stop();
        if (_viewportRenderPending)
            ApplyViewportVirtualization();
    }

    private void ApplyViewportVirtualization()
    {
        if (_viewportRenderRunning || _viewportVirtualizationSuspended || _document is null || _nodeDragActive || MapHost.Visibility != Visibility.Visible)
            return;

        var zoom = Math.Max(0.01, MapScrollViewer.ZoomFactor);
        var viewportWidth = MapScrollViewer.ViewportWidth / zoom;
        var viewportHeight = MapScrollViewer.ViewportHeight / zoom;
        if (viewportWidth <= 1 || viewportHeight <= 1)
        {
            _viewportRenderPending = true;
            _viewportRenderTimer.Stop();
            _viewportRenderTimer.Start();
            return;
        }

        _viewportRenderRunning = true;
        _viewportRenderPending = false;
        try
        {
            var snapshot = new RightLogicLayoutStrategy().Arrange(_document);
            var viewport = new RectD(
                MapScrollViewer.HorizontalOffset / zoom,
                MapScrollViewer.VerticalOffset / zoom,
                viewportWidth,
                viewportHeight);
            var slice = _viewportLayoutFilter.Filter(snapshot, viewport, _selectedNodeId);
            if (!slice.IsVirtualized)
            {
                _viewportVirtualized = false;
                return;
            }

            MapCanvas.Width = Math.Max(1200, snapshot.CanvasBounds.Width);
            MapCanvas.Height = Math.Max(760, snapshot.CanvasBounds.Height);
            RemoveRetainedMapVisuals();
            DrawViewportGrid(viewport, 220);
            DrawViewportConnectors(slice);
            DrawViewportNodes(slice);
            _viewportVirtualized = true;
        }
        finally
        {
            _viewportRenderRunning = false;
        }
    }

    private bool SuspendVirtualizationForFullCanvas()
    {
        var restore = _viewportVirtualized;
        _viewportVirtualizationSuspended = true;
        _viewportRenderTimer.Stop();
        _viewportRenderPending = false;
        if (_document is not null && restore)
        {
            RenderMap(_document);
            RefreshNodeDecorations();
        }
        return restore;
    }

    private void ResumeVirtualizationAfterFullCanvas(bool restore)
    {
        _viewportVirtualizationSuspended = false;
        if (restore)
            ScheduleViewportRender(immediate: true);
    }

    private void RemoveRetainedMapVisuals()
    {
        for (var index = MapCanvas.Children.Count - 1; index >= 0; index--)
        {
            var child = MapCanvas.Children[index];
            if (child is Line || child is Border { Tag: Guid })
                MapCanvas.Children.RemoveAt(index);
        }
    }

    private void DrawViewportGrid(RectD viewport, double overscan)
    {
        const double spacing = 40;
        var brush = new SolidColorBrush(ColorHelper.FromArgb(192, 237, 237, 237));
        var left = Math.Max(0, viewport.X - overscan);
        var top = Math.Max(0, viewport.Y - overscan);
        var right = Math.Min(MapCanvas.Width, viewport.Right + overscan);
        var bottom = Math.Min(MapCanvas.Height, viewport.Bottom + overscan);
        var firstX = Math.Floor(left / spacing) * spacing;
        var firstY = Math.Floor(top / spacing) * spacing;

        for (var x = Math.Max(spacing, firstX); x <= right; x += spacing)
        {
            MapCanvas.Children.Add(new Line
            {
                X1 = x,
                Y1 = top,
                X2 = x,
                Y2 = bottom,
                Stroke = brush,
                StrokeThickness = 1,
                IsHitTestVisible = false
            });
        }

        for (var y = Math.Max(spacing, firstY); y <= bottom; y += spacing)
        {
            MapCanvas.Children.Add(new Line
            {
                X1 = left,
                Y1 = y,
                X2 = right,
                Y2 = y,
                Stroke = brush,
                StrokeThickness = 1,
                IsHitTestVisible = false
            });
        }
    }

    private void DrawViewportConnectors(ViewportLayoutSlice slice)
    {
        foreach (var connector in slice.Connectors)
        {
            MapCanvas.Children.Add(new Line
            {
                X1 = connector.StartX,
                Y1 = connector.StartY,
                X2 = connector.EndX,
                Y2 = connector.EndY,
                Stroke = ResourceBrush("V4NodeAccentBlueBrush", ColorHelper.FromArgb(255, 8, 107, 194)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            });
        }
    }

    private void DrawViewportNodes(ViewportLayoutSlice slice)
    {
        if (_document is null)
            return;

        foreach (var layout in slice.Nodes.Values)
        {
            var node = _document.GetNode(layout.NodeId);
            var isSelected = _functionalSelectedNodeIds.Count > 0
                ? _functionalSelectedNodeIds.Contains(node.Id)
                : node.Id == _selectedNodeId;
            var border = new Border
            {
                Width = layout.Bounds.Width,
                Height = layout.Bounds.Height,
                CornerRadius = new CornerRadius(6),
                Background = isSelected
                    ? ResourceBrush("V4ControlSelectedBackgroundBrush", ColorHelper.FromArgb(255, 231, 241, 251))
                    : ResourceBrush("V4NodeBackgroundBrush", Colors.White),
                BorderBrush = isSelected
                    ? ResourceBrush("V4ControlSelectedStrokeBrush", ColorHelper.FromArgb(255, 96, 166, 230))
                    : ResourceBrush("V4NodeStrokeBrush", ColorHelper.FromArgb(255, 235, 235, 235)),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                Tag = node.Id
            };
            border.PointerPressed += MapNode_PointerPressed;
            border.DoubleTapped += MapNode_DoubleTapped;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var accent = new Border
            {
                Background = ResourceBrush("V4NodeAccentBlueBrush", ColorHelper.FromArgb(255, 8, 107, 194)),
                CornerRadius = new CornerRadius(2, 0, 0, 2),
                IsHitTestVisible = false
            };
            grid.Children.Add(accent);
            Grid.SetColumn(accent, 0);

            var text = new TextBlock
            {
                Text = node.Title,
                FontSize = 14,
                Foreground = ResourceBrush("V4TextStrongBrush", ColorHelper.FromArgb(255, 26, 28, 33)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 12, 0),
                FontWeight = layout.Depth == 0
                    ? Microsoft.UI.Text.FontWeights.SemiBold
                    : Microsoft.UI.Text.FontWeights.Normal,
                IsHitTestVisible = false,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);
            ApplyNodeDecoration(grid, node);

            border.Child = grid;
            Canvas.SetLeft(border, layout.Bounds.X);
            Canvas.SetTop(border, layout.Bounds.Y);
            MapCanvas.Children.Add(border);
        }
    }
}
