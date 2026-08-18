using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MindCanvas.Core.Commands;
using Windows.System;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    private readonly HashSet<Guid> _functionalSelectedNodeIds = [];
    private bool _multiSelectionInitialized;

    public IReadOnlyCollection<Guid> SelectedNodeIds => _functionalSelectedNodeIds.ToArray();

    public void InitializeMultiSelection()
    {
        if (_multiSelectionInitialized)
            return;

        _multiSelectionInitialized = true;
        if (_selectedNodeId is Guid selectedId)
            _functionalSelectedNodeIds.Add(selectedId);

        SelectionChanged += MultiSelection_SelectionChanged;
        MapCanvas.AddHandler(
            UIElement.PointerPressedEvent,
            new PointerEventHandler(MapCanvas_MultiSelectPointerPressed),
            handledEventsToo: true);
        PreviewKeyDown += MultiSelection_PreviewKeyDown;
        ApplyFunctionalSelectionVisuals();
    }

    private void MultiSelection_SelectionChanged(object? sender, EventArgs e)
    {
        if (_document is null || _selectedNodeId is not Guid selectedId)
            return;

        if (!IsKeyDown(VirtualKey.Control))
        {
            _functionalSelectedNodeIds.Clear();
            _functionalSelectedNodeIds.Add(selectedId);
        }
        else if (_functionalSelectedNodeIds.Count == 0)
        {
            _functionalSelectedNodeIds.Add(selectedId);
        }

        ApplyFunctionalSelectionVisuals();
    }

    private void MapCanvas_MultiSelectPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_document is null || !TryGetNodeId(e.OriginalSource, out var nodeId))
            return;

        if (!IsKeyDown(VirtualKey.Control))
        {
            _functionalSelectedNodeIds.Clear();
            _functionalSelectedNodeIds.Add(nodeId);
            ApplyFunctionalSelectionVisuals();
            return;
        }

        if (_functionalSelectedNodeIds.Contains(nodeId))
        {
            if (_functionalSelectedNodeIds.Count > 1)
                _functionalSelectedNodeIds.Remove(nodeId);
        }
        else
        {
            _functionalSelectedNodeIds.Add(nodeId);
        }

        ApplyFunctionalSelectionVisuals();
    }

    private void MultiSelection_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_document is null)
            return;

        var control = IsKeyDown(VirtualKey.Control);
        if (control && e.Key == VirtualKey.A)
        {
            _functionalSelectedNodeIds.Clear();
            foreach (var node in _document.EnumerateVisibleDepthFirst())
                _functionalSelectedNodeIds.Add(node.Id);
            if (_selectedNodeId is null)
                _selectedNodeId = _document.RootNodeId;
            ApplyFunctionalSelectionVisuals();
            e.Handled = true;
            return;
        }

        if (e.Key == VirtualKey.Escape && _functionalSelectedNodeIds.Count > 1)
        {
            CollapseFunctionalSelectionToPrimary();
            e.Handled = true;
            return;
        }

        if (e.Key == VirtualKey.Delete && _functionalSelectedNodeIds.Count > 1)
        {
            e.Handled = DeleteFunctionalSelection();
        }
    }

    private bool DeleteFunctionalSelection()
    {
        if (_document is null || _history is null || _functionalSelectedNodeIds.Count <= 1)
            return false;

        var deletable = _functionalSelectedNodeIds
            .Where(id => id != _document.RootNodeId && _document.Nodes.ContainsKey(id))
            .ToArray();
        if (deletable.Length == 0)
            return false;

        Guid fallback = _document.RootNodeId;
        if (_selectedNodeId is Guid primary && _document.Nodes.TryGetValue(primary, out var selectedNode) && selectedNode.ParentId is Guid parentId)
            fallback = parentId;

        _history.Execute(new DeleteNodesCommand(_document, deletable));
        _functionalSelectedNodeIds.Clear();
        _selectedNodeId = _document.Nodes.ContainsKey(fallback) ? fallback : _document.RootNodeId;
        _functionalSelectedNodeIds.Add(_selectedNodeId.Value);
        NotifyMutation();
        ApplyFunctionalSelectionVisuals();
        return true;
    }

    private void CollapseFunctionalSelectionToPrimary()
    {
        _functionalSelectedNodeIds.Clear();
        if (_selectedNodeId is Guid selectedId)
            _functionalSelectedNodeIds.Add(selectedId);
        ApplyFunctionalSelectionVisuals();
    }

    private void ApplyFunctionalSelectionVisuals()
    {
        if (_document is null)
            return;

        foreach (var border in MapCanvas.Children.OfType<Border>())
        {
            if (border.Tag is not Guid nodeId)
                continue;

            var selected = _functionalSelectedNodeIds.Contains(nodeId);
            border.Background = selected
                ? ResourceBrush("V4ControlSelectedBackgroundBrush", Microsoft.UI.ColorHelper.FromArgb(255, 231, 241, 251))
                : ResourceBrush("V4NodeBackgroundBrush", Microsoft.UI.Colors.White);
            border.BorderBrush = selected
                ? ResourceBrush("V4ControlSelectedStrokeBrush", Microsoft.UI.ColorHelper.FromArgb(255, 96, 166, 230))
                : ResourceBrush("V4NodeStrokeBrush", Microsoft.UI.ColorHelper.FromArgb(255, 235, 235, 235));
            border.BorderThickness = new Thickness(selected ? 2 : 1);
        }

        if (_functionalSelectedNodeIds.Count > 1)
        {
            MapStatusText.Text = T(
                $"{_functionalSelectedNodeIds.Count} nodes selected · Delete to remove · Esc to clear",
                $"已选择 {_functionalSelectedNodeIds.Count} 个节点 · Delete 删除 · Esc 取消多选",
                $"{_functionalSelectedNodeIds.Count} ノード選択中 · Delete で削除 · Esc で解除");
        }
    }

    private bool TryGetNodeId(object? source, out Guid nodeId)
    {
        var current = source as DependencyObject;
        while (current is not null && current != MapCanvas)
        {
            if (current is FrameworkElement { Tag: Guid id } && _document?.Nodes.ContainsKey(id) == true)
            {
                nodeId = id;
                return true;
            }
            current = VisualTreeHelper.GetParent(current);
        }

        nodeId = Guid.Empty;
        return false;
    }
}
