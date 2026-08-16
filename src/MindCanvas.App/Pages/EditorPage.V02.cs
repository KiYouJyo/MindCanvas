using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using Windows.System;
using Windows.UI.Core;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    private const float ZoomStep = 0.10f;
    private static NodeSubtreeTemplate? _subtreeClipboard;
    private bool _v02HooksInitialized;

    public bool CanPaste => _subtreeClipboard is not null;

    private void EditorPage_V02Loaded(object sender, RoutedEventArgs e)
    {
        if (_v02HooksInitialized)
            return;

        _v02HooksInitialized = true;
        KeyDown += EditorSurfaceV02_KeyDown;
        ZoomText.Text = $"{MapScrollViewer.ZoomFactor * 100:0}%";
        FitViewButton.Content = T("Fit", "适应", "全体");
    }

    public bool CopySelected()
    {
        if (_document is null || _selectedNodeId is not Guid selectedId || !_document.Nodes.ContainsKey(selectedId))
            return false;

        _subtreeClipboard = NodeSubtreeTemplate.Capture(_document, selectedId);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool CutSelected()
    {
        if (_document is null || _selectedNodeId is not Guid selectedId || selectedId == _document.RootNodeId)
            return false;

        _subtreeClipboard = NodeSubtreeTemplate.Capture(_document, selectedId);
        return DeleteSelected();
    }

    public Guid? PasteClipboard()
    {
        if (_document is null || _history is null || _subtreeClipboard is null)
            return null;

        var parentId = _selectedNodeId is Guid selectedId && _document.Nodes.ContainsKey(selectedId)
            ? selectedId
            : _document.RootNodeId;
        var command = new InsertSubtreeCommand(_document, parentId, _subtreeClipboard);
        _history.Execute(command);
        _selectedNodeId = command.CreatedRootId ?? parentId;
        NotifyMutation();
        return command.CreatedRootId;
    }

    public Guid? DuplicateSelected()
    {
        if (_document is null || _history is null || _selectedNodeId is not Guid selectedId || selectedId == _document.RootNodeId)
            return null;

        var selected = _document.GetNode(selectedId);
        var parentId = selected.ParentId ?? _document.RootNodeId;
        var siblings = _document.GetNode(parentId).ChildrenIds;
        var selectedIndex = siblings.IndexOf(selectedId);
        var template = NodeSubtreeTemplate.Capture(_document, selectedId);
        var command = new InsertSubtreeCommand(
            _document,
            parentId,
            template,
            selectedIndex >= 0 ? selectedIndex + 1 : null);
        _history.Execute(command);
        _selectedNodeId = command.CreatedRootId ?? selectedId;
        NotifyMutation();
        return command.CreatedRootId;
    }

    public bool MoveSelectedUp() => MoveSelectedAmongSiblings(-1);

    public bool MoveSelectedDown() => MoveSelectedAmongSiblings(1);

    public bool IndentSelected()
    {
        if (_document is null || _history is null || _selectedNodeId is not Guid selectedId || selectedId == _document.RootNodeId)
            return false;

        var selected = _document.GetNode(selectedId);
        if (selected.ParentId is not Guid parentId)
            return false;

        var siblings = _document.GetNode(parentId).ChildrenIds;
        var index = siblings.IndexOf(selectedId);
        if (index <= 0)
            return false;

        var newParentId = siblings[index - 1];
        var newParent = _document.GetNode(newParentId);
        _history.Execute(new MoveNodeCommand(_document, selectedId, newParentId, newParent.ChildrenIds.Count));
        NotifyMutation();
        return true;
    }

    public bool OutdentSelected()
    {
        if (_document is null || _history is null || _selectedNodeId is not Guid selectedId || selectedId == _document.RootNodeId)
            return false;

        var selected = _document.GetNode(selectedId);
        if (selected.ParentId is not Guid parentId || parentId == _document.RootNodeId)
            return false;

        var parent = _document.GetNode(parentId);
        if (parent.ParentId is not Guid grandParentId)
            return false;

        var grandParent = _document.GetNode(grandParentId);
        var parentIndex = grandParent.ChildrenIds.IndexOf(parentId);
        _history.Execute(new MoveNodeCommand(
            _document,
            selectedId,
            grandParentId,
            parentIndex >= 0 ? parentIndex + 1 : null));
        NotifyMutation();
        return true;
    }

    private bool MoveSelectedAmongSiblings(int delta)
    {
        if (_document is null || _history is null || _selectedNodeId is not Guid selectedId || selectedId == _document.RootNodeId)
            return false;

        var selected = _document.GetNode(selectedId);
        if (selected.ParentId is not Guid parentId)
            return false;

        var siblings = _document.GetNode(parentId).ChildrenIds;
        var index = siblings.IndexOf(selectedId);
        if (index < 0)
            return false;

        var target = index + delta;
        if (target < 0 || target >= siblings.Count)
            return false;

        // MoveNode removes the current node first, so the desired adjacent
        // position is the target index in the post-removal sibling list.
        _history.Execute(new MoveNodeCommand(_document, selectedId, parentId, target));
        NotifyMutation();
        return true;
    }

    private bool NavigateVisible(int delta)
    {
        if (_document is null)
            return false;

        var visible = _document.EnumerateVisibleDepthFirst().Select(node => node.Id).ToArray();
        if (visible.Length == 0)
            return false;

        var currentIndex = _selectedNodeId is Guid selectedId ? Array.IndexOf(visible, selectedId) : -1;
        var nextIndex = Math.Clamp(currentIndex < 0 ? 0 : currentIndex + delta, 0, visible.Length - 1);
        if (currentIndex == nextIndex)
            return false;

        SelectNode(visible[nextIndex]);
        return true;
    }

    private bool NavigateLeft()
    {
        if (_document is null || _selectedNodeId is not Guid selectedId)
            return false;

        var selected = _document.GetNode(selectedId);
        if (selected.ChildrenIds.Count > 0 && !selected.IsCollapsed)
            return SetSelectedCollapsed(true);

        if (selected.ParentId is Guid parentId)
        {
            SelectNode(parentId);
            return true;
        }

        return false;
    }

    private bool NavigateRight()
    {
        if (_document is null || _selectedNodeId is not Guid selectedId)
            return false;

        var selected = _document.GetNode(selectedId);
        if (selected.ChildrenIds.Count == 0)
            return false;

        if (selected.IsCollapsed)
            return SetSelectedCollapsed(false);

        SelectNode(selected.ChildrenIds[0]);
        return true;
    }

    private void EditorSurfaceV02_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || e.OriginalSource is TextBox)
            return;

        var control = IsKeyDown(VirtualKey.Control);
        var alt = IsKeyDown(VirtualKey.Menu);

        if (control)
        {
            switch (e.Key)
            {
                case VirtualKey.C:
                    e.Handled = CopySelected();
                    return;
                case VirtualKey.X:
                    e.Handled = CutSelected();
                    return;
                case VirtualKey.V:
                    e.Handled = PasteClipboard() is not null;
                    return;
                case VirtualKey.D:
                    e.Handled = DuplicateSelected() is not null;
                    return;
                case VirtualKey.Z:
                    e.Handled = Undo();
                    return;
                case VirtualKey.Y:
                    e.Handled = Redo();
                    return;
            }
        }

        if (alt)
        {
            e.Handled = e.Key switch
            {
                VirtualKey.Up => MoveSelectedUp(),
                VirtualKey.Down => MoveSelectedDown(),
                VirtualKey.Left => OutdentSelected(),
                VirtualKey.Right => IndentSelected(),
                _ => false
            };
            return;
        }

        e.Handled = e.Key switch
        {
            VirtualKey.Up => NavigateVisible(-1),
            VirtualKey.Down => NavigateVisible(1),
            VirtualKey.Left => NavigateLeft(),
            VirtualKey.Right => NavigateRight(),
            _ => false
        };
    }

    private static bool IsKeyDown(VirtualKey key)
        => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        => SetZoom(MapScrollViewer.ZoomFactor - ZoomStep);

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        => SetZoom(MapScrollViewer.ZoomFactor + ZoomStep);

    private void FitViewButton_Click(object sender, RoutedEventArgs e)
    {
        if (MapCanvas.Width <= 0 || MapCanvas.Height <= 0 || MapScrollViewer.ViewportWidth <= 0 || MapScrollViewer.ViewportHeight <= 0)
            return;

        var horizontal = MapScrollViewer.ViewportWidth / MapCanvas.Width;
        var vertical = MapScrollViewer.ViewportHeight / MapCanvas.Height;
        var factor = (float)(Math.Min(horizontal, vertical) * 0.92);
        var clamped = Math.Clamp(factor, MapScrollViewer.MinZoomFactor, MapScrollViewer.MaxZoomFactor);
        MapScrollViewer.ChangeView(0, 0, clamped, disableAnimation: false);
    }

    private void MapScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        => ZoomText.Text = $"{MapScrollViewer.ZoomFactor * 100:0}%";

    private void SetZoom(float factor)
    {
        var clamped = Math.Clamp(factor, MapScrollViewer.MinZoomFactor, MapScrollViewer.MaxZoomFactor);
        MapScrollViewer.ChangeView(null, null, clamped, disableAnimation: false);
    }
}
