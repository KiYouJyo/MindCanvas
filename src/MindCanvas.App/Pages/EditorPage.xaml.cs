using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using MindCanvas.Theming;
using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using MindCanvas.Layout;
using Windows.System;

namespace MindCanvas.Pages;

public sealed partial class EditorPage : Page
{
    private readonly Dictionary<TreeViewNode, Guid> _outlineNodeIds = [];
    private readonly Dictionary<Guid, TreeViewNode> _outlineNodesById = [];
    private MindMapDocument? _document;
    private UndoRedoManager? _history;
    private string _mode = "split";
    private bool _formatVisible = true;
    private bool _syncingOutlineSelection;
    private Guid? _selectedNodeId;

    public EditorPage()
    {
        InitializeComponent();
        IsTabStop = true;
        OutlineTree.SelectionChanged += OutlineTree_SelectionChanged;
        OutlineAddTopicButton.Click += OutlineAddTopicButton_Click;
        KeyDown += EditorSurface_KeyDown;
        SetFormatTexts();
        SetView("split");
    }

    public Guid? SelectedNodeId => _selectedNodeId;

    public event EventHandler? SelectionChanged;
    public event EventHandler? DocumentChanged;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var navigation = e.Parameter switch
        {
            EditorNavigation nav => nav,
            MindMapDocument document => new EditorNavigation(document, null, "split", document.RootNodeId),
            _ => new EditorNavigation(null, null, "split", null)
        };

        _document = navigation.Document;
        _history = navigation.History;
        _selectedNodeId = navigation.SelectedNodeId ?? _document?.RootNodeId;
        SetView(navigation.Mode);
    }

    public void SetView(string mode)
    {
        _mode = mode is "map" or "outline" or "split" ? mode : "split";

        switch (_mode)
        {
            case "outline":
                OutlineColumn.Width = new GridLength(0);
                NarrowOutlinePane.Visibility = Visibility.Collapsed;
                WideOutlinePane.Visibility = Visibility.Visible;
                MapHost.Visibility = Visibility.Collapsed;
                break;

            case "map":
                OutlineColumn.Width = new GridLength(0);
                NarrowOutlinePane.Visibility = Visibility.Collapsed;
                WideOutlinePane.Visibility = Visibility.Collapsed;
                MapHost.Visibility = Visibility.Visible;
                break;

            default:
                OutlineColumn.Width = new GridLength(306);
                NarrowOutlinePane.Visibility = Visibility.Visible;
                WideOutlinePane.Visibility = Visibility.Collapsed;
                MapHost.Visibility = Visibility.Visible;
                break;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (_document is null)
            return;

        if (_selectedNodeId is not Guid selected || !_document.Nodes.ContainsKey(selected))
            _selectedNodeId = _document.RootNodeId;

        RenderMap(_document);
        PopulateOutline(_document);
        PopulateWideOutline(_document);
        SetFormatTexts();
    }

    public void SelectNode(Guid? nodeId)
    {
        if (_document is null)
            return;

        var next = nodeId is Guid id && _document.Nodes.ContainsKey(id)
            ? id
            : _document.RootNodeId;

        if (_selectedNodeId == next)
            return;

        _selectedNodeId = next;
        RenderMap(_document);
        SyncOutlineSelection();
        PopulateWideOutline(_document);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public Guid? AddRootTopic(string title)
    {
        if (_document is null || _history is null)
            return null;

        int? index = null;
        if (_selectedNodeId is Guid selectedId && selectedId != _document.RootNodeId)
        {
            var selected = _document.GetNode(selectedId);
            if (selected.ParentId == _document.RootNodeId)
            {
                var selectedIndex = _document.Root.ChildrenIds.IndexOf(selectedId);
                if (selectedIndex >= 0)
                    index = selectedIndex + 1;
            }
        }

        return ExecuteAdd(_document.RootNodeId, title, index);
    }

    public Guid? AddSubtopic(string title)
    {
        if (_document is null || _history is null)
            return null;

        var parentId = _selectedNodeId is Guid selectedId && _document.Nodes.ContainsKey(selectedId)
            ? selectedId
            : _document.RootNodeId;
        return ExecuteAdd(parentId, title, null);
    }

    public Guid? AddSibling(string title)
    {
        if (_document is null || _history is null)
            return null;

        if (_selectedNodeId is not Guid selectedId || !_document.Nodes.ContainsKey(selectedId) || selectedId == _document.RootNodeId)
            return ExecuteAdd(_document.RootNodeId, title, null);

        var selected = _document.GetNode(selectedId);
        var parentId = selected.ParentId ?? _document.RootNodeId;
        var siblings = _document.GetNode(parentId).ChildrenIds;
        var selectedIndex = siblings.IndexOf(selectedId);
        return ExecuteAdd(parentId, title, selectedIndex >= 0 ? selectedIndex + 1 : null);
    }

    public bool DeleteSelected()
    {
        if (_document is null || _history is null || _selectedNodeId is not Guid selectedId || selectedId == _document.RootNodeId)
            return false;

        var parentId = _document.GetNode(selectedId).ParentId ?? _document.RootNodeId;
        _history.Execute(new DeleteNodeCommand(_document, selectedId));
        _selectedNodeId = parentId;
        NotifyMutation();
        return true;
    }

    public bool SetSelectedCollapsed(bool collapsed)
    {
        if (_document is null || _history is null || _selectedNodeId is not Guid selectedId)
            return false;

        var node = _document.GetNode(selectedId);
        if (node.ChildrenIds.Count == 0 || node.IsCollapsed == collapsed)
            return false;

        _history.Execute(new SetNodeCollapsedCommand(_document, selectedId, collapsed));
        NotifyMutation();
        return true;
    }

    public bool Undo()
    {
        if (_history?.Undo() != true)
            return false;

        NotifyMutation();
        return true;
    }

    public bool Redo()
    {
        if (_history?.Redo() != true)
            return false;

        NotifyMutation();
        return true;
    }

    public async Task<bool> RenameSelectedAsync()
    {
        if (_document is null || _selectedNodeId is not Guid selectedId || !_document.Nodes.ContainsKey(selectedId))
            return false;

        var node = _document.GetNode(selectedId);
        var editor = new TextBox
        {
            Text = node.Title,
            MinWidth = 320,
            SelectionStart = 0,
            SelectionLength = node.Title.Length
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = T("Rename topic", "重命名主题", "トピック名を変更"),
            Content = editor,
            PrimaryButtonText = T("Save", "保存", "保存"),
            CloseButtonText = T("Cancel", "取消", "キャンセル"),
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        var title = editor.Text.Trim();
        if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(title) || title == node.Title)
            return false;

        if (_history is not null)
            _history.Execute(new RenameNodeCommand(_document, selectedId, title));
        else
            _document.RenameNode(selectedId, title);

        NotifyMutation();
        return true;
    }

    public void ToggleFormatPanel()
    {
        _formatVisible = !_formatVisible;
        FormatPanel.Visibility = _formatVisible ? Visibility.Visible : Visibility.Collapsed;
        FormatColumn.Width = _formatVisible ? new GridLength(260) : new GridLength(0);
    }

    public void ToggleCollapse(bool collapse) => SetSelectedCollapsed(collapse);

    private void CollapseFormatButton_Click(object sender, RoutedEventArgs e) => ToggleFormatPanel();

    private Guid? ExecuteAdd(Guid parentId, string title, int? index)
    {
        if (_document is null || _history is null)
            return null;

        var command = new AddNodeCommand(_document, parentId, title, index);
        _history.Execute(command);
        _selectedNodeId = command.CreatedNodeId ?? parentId;
        NotifyMutation();
        return command.CreatedNodeId;
    }

    private void NotifyMutation()
    {
        Refresh();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        DocumentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RenderMap(MindMapDocument document)
    {
        MapCanvas.Children.Clear();
        var snapshot = new RightLogicLayoutStrategy().Arrange(document);
        MapCanvas.Width = Math.Max(1200, snapshot.CanvasBounds.Width);
        MapCanvas.Height = Math.Max(760, snapshot.CanvasBounds.Height);

        DrawGrid();

        foreach (var connector in snapshot.Connectors)
        {
            var line = new Line
            {
                X1 = connector.StartX,
                Y1 = connector.StartY,
                X2 = connector.EndX,
                Y2 = connector.EndY,
                Stroke = ResourceBrush("V4NodeAccentBlueBrush", ColorHelper.FromArgb(255, 8, 107, 194)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            MapCanvas.Children.Add(line);
        }

        foreach (var layout in snapshot.Nodes.Values)
        {
            var node = document.GetNode(layout.NodeId);
            var isSelected = node.Id == _selectedNodeId;
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
                IsHitTestVisible = false
            };
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            border.Child = grid;
            Canvas.SetLeft(border, layout.Bounds.X);
            Canvas.SetTop(border, layout.Bounds.Y);
            MapCanvas.Children.Add(border);
        }
    }

    private void DrawGrid()
    {
        var gridColor = ColorHelper.FromArgb(192, 237, 237, 237);
        var brush = new SolidColorBrush(gridColor);
        for (var x = 40; x < MapCanvas.Width; x += 40)
        {
            MapCanvas.Children.Add(new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = MapCanvas.Height,
                Stroke = brush,
                StrokeThickness = 1,
                IsHitTestVisible = false
            });
        }
        for (var y = 40; y < MapCanvas.Height; y += 40)
        {
            MapCanvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = MapCanvas.Width,
                Y2 = y,
                Stroke = brush,
                StrokeThickness = 1,
                IsHitTestVisible = false
            });
        }
    }

    private void MapNode_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
        {
            Focus(FocusState.Programmatic);
            SelectNode(id);
            e.Handled = true;
        }
    }

    private async void MapNode_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
        {
            SelectNode(id);
            e.Handled = true;
            await RenameSelectedAsync();
        }
    }

    private void PopulateOutline(MindMapDocument document)
    {
        _syncingOutlineSelection = true;
        try
        {
            OutlineTree.RootNodes.Clear();
            _outlineNodeIds.Clear();
            _outlineNodesById.Clear();

            TreeViewNode Build(Guid id)
            {
                var node = document.GetNode(id);
                var result = new TreeViewNode { Content = node.Title, IsExpanded = !node.IsCollapsed };
                _outlineNodeIds[result] = id;
                _outlineNodesById[id] = result;
                foreach (var child in node.ChildrenIds)
                    result.Children.Add(Build(child));
                return result;
            }

            OutlineTree.RootNodes.Add(Build(document.RootNodeId));
            SyncOutlineSelection();
        }
        finally
        {
            _syncingOutlineSelection = false;
        }
    }

    private void SyncOutlineSelection()
    {
        if (_selectedNodeId is Guid selectedId && _outlineNodesById.TryGetValue(selectedId, out var treeNode))
        {
            var previous = _syncingOutlineSelection;
            _syncingOutlineSelection = true;
            OutlineTree.SelectedNode = treeNode;
            _syncingOutlineSelection = previous;
        }
    }

    private void OutlineTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (_syncingOutlineSelection)
            return;

        if (sender.SelectedNode is TreeViewNode node && _outlineNodeIds.TryGetValue(node, out var id))
            SelectNode(id);
    }

    private void OutlineAddTopicButton_Click(object sender, RoutedEventArgs e)
        => AddSubtopic(T("New topic", "新主题", "新規トピック"));

    private void PopulateWideOutline(MindMapDocument document)
    {
        WideOutlineRows.Children.Clear();
        var index = 0;
        foreach (var node in document.EnumerateVisibleDepthFirst())
        {
            var isSelected = node.Id == _selectedNodeId;
            var row = new Border
            {
                Height = 54,
                CornerRadius = new CornerRadius(4),
                Background = isSelected
                    ? ResourceBrush("V4ControlSelectedBackgroundBrush", ColorHelper.FromArgb(255, 231, 241, 251))
                    : ResourceBrush("V4CardBackgroundBrush", Colors.White),
                BorderBrush = isSelected
                    ? ResourceBrush("V4ControlSelectedStrokeBrush", ColorHelper.FromArgb(255, 96, 166, 230))
                    : ResourceBrush("V4CardStrokeBrush", ColorHelper.FromArgb(255, 235, 235, 235)),
                BorderThickness = new Thickness(1),
                Tag = node.Id
            };
            row.PointerPressed += WideOutlineRow_PointerPressed;
            row.DoubleTapped += WideOutlineRow_DoubleTapped;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(116) });

            var title = new TextBlock
            {
                Text = node.Title,
                FontSize = 14,
                Foreground = isSelected
                    ? ResourceBrush("V4AccentForegroundBrush", ColorHelper.FromArgb(255, 0, 99, 186))
                    : ResourceBrush("V4TextStrongBrush", ColorHelper.FromArgb(255, 23, 26, 31)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(18 + GetDepth(document, node) * 18, 0, 0, 0),
                IsHitTestVisible = false
            };
            grid.Children.Add(title);
            Grid.SetColumn(title, 0);

            var note = new TextBlock
            {
                Text = node.Notes ?? T("Project overview", "项目总览", "プロジェクト概要"),
                FontSize = 12,
                Foreground = ResourceBrush("V4TextSecondaryBrush", ColorHelper.FromArgb(255, 97, 105, 117)),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            grid.Children.Add(note);
            Grid.SetColumn(note, 1);

            var statusBorder = new Border
            {
                Width = 116,
                Height = 30,
                CornerRadius = new CornerRadius(4),
                Background = (index % 4) switch
                {
                    1 => ResourceBrush("V4StatusDoneBackgroundBrush", ColorHelper.FromArgb(255, 229, 247, 229)),
                    2 => ResourceBrush("V4StatusWarnBackgroundBrush", ColorHelper.FromArgb(255, 255, 242, 222)),
                    3 => ResourceBrush("V4StatusTodoBackgroundBrush", ColorHelper.FromArgb(255, 242, 237, 252)),
                    _ => ResourceBrush("V4StatusProgressBackgroundBrush", ColorHelper.FromArgb(255, 224, 240, 255))
                },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = (index % 4) switch
                    {
                        1 => T("Done", "已完成", "完了"),
                        2 => T("In progress", "进行中", "進行中"),
                        3 => T("To do", "待开始", "未着手"),
                        _ => T("In progress", "进行中", "進行中")
                    },
                    FontSize = 12,
                    Foreground = ResourceBrush("V4TextMutedBrush", ColorHelper.FromArgb(255, 94, 94, 94)),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
            Grid.SetColumn(statusBorder, 2);
            grid.Children.Add(statusBorder);

            row.Child = grid;
            WideOutlineRows.Children.Add(row);
            index++;
        }
    }

    private void WideOutlineRow_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
        {
            SelectNode(id);
            e.Handled = true;
        }
    }

    private async void WideOutlineRow_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
        {
            SelectNode(id);
            e.Handled = true;
            await RenameSelectedAsync();
        }
    }

    private async void EditorSurface_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.OriginalSource is TextBox)
            return;

        switch (e.Key)
        {
            case VirtualKey.Enter:
                AddSibling(T("Sibling", "同级主题", "同階層トピック"));
                e.Handled = true;
                break;
            case VirtualKey.Tab:
            case VirtualKey.Insert:
                AddSubtopic(T("Subtopic", "子主题", "サブトピック"));
                e.Handled = true;
                break;
            case VirtualKey.Delete:
                e.Handled = DeleteSelected();
                break;
            case VirtualKey.F2:
                e.Handled = true;
                await RenameSelectedAsync();
                break;
        }
    }

    private void SetFormatTexts()
    {
        OutlinePaneTitle.Text = T("Outline", "大纲", "アウトライン");
        OutlineSearch.PlaceholderText = T("Search nodes", "搜索节点", "ノードを検索");
        OutlineAddTopicButton.Content = "＋  " + T("New topic", "新主题", "新規トピック");

        WideOutlineTitle.Text = T("Outline", "大纲", "アウトライン");
        if (_document is not null)
        {
            var visibleCount = _document.EnumerateVisibleDepthFirst().Count();
            var collapsedCount = _document.Nodes.Values.Count(n => n.IsCollapsed && n.ChildrenIds.Count > 0);
            WideOutlineCount.Text = T(
                $"{visibleCount} topics · {collapsedCount} collapsed branches",
                $"{visibleCount} 个主题 · {collapsedCount} 个折叠分支",
                $"{visibleCount} トピック · {collapsedCount} 件の折りたたみブランチ");
        }
        else
        {
            WideOutlineCount.Text = T("0 topics", "0 个主题", "0 トピック");
        }
        WideOutlineSearch.PlaceholderText = T("Search topics or notes", "搜索主题或备注", "トピックまたはノートを検索");
        WideOutlineDensityLabel.Text = T("Density", "密度", "密度");
        CompactDensityButton.Content = T("Compact", "紧凑", "コンパクト");
        ComfortableDensityButton.Content = T("Comfortable", "舒适", "標準");
        WideOutlineTopicHeader.Text = T("Topic", "主题", "トピック");
        WideOutlineNoteHeader.Text = T("Note / summary", "备注 / 摘要", "ノート / 概要");
        WideOutlineStatusHeader.Text = T("Status", "状态", "状態");

        FormatPanelTitle.Text = T("Format", "格式", "書式");
        CollapseFormatButton.Content = "›";
        StructureTabButton.Content = T("Structure", "结构", "構造");
        ThemeTabButton.Content = T("Theme", "主题", "テーマ");
        CurrentTemplateLabel.Text = T("Current template", "当前模板", "現在のテンプレート");
        CurrentTemplateName.Text = T("Project plan", "项目计划", "プロジェクト計画");
        CurrentTemplateHint.Text = T("Mind map structure", "思维导图结构", "マインドマップ構造");
        ChangeTemplateButton.Content = T("Change", "更换", "変更");
        StructureLabel.Text = T("Structure", "结构", "構造");
        StructureMindMapText.Text = T("Mind map", "思维导图", "マインドマップ");
        StructureLogicText.Text = T("Logic chart", "逻辑图", "ロジック図");
        StructureTreeText.Text = T("Tree chart", "树状图", "ツリー図");
        StructureOrgText.Text = T("Org chart", "组织图", "組織図");
        ThemeLabel.Text = T("Theme", "主题", "テーマ");
        ThemeFluentText.Text = T("Fluent light", "Fluent 浅色", "Fluent ライト");
        ThemeSoftText.Text = T("Soft", "柔和", "ソフト");
        ThemeMonoText.Text = T("Mono", "单色", "モノ");
        ThemeDarkText.Text = T("Dark", "深色", "ダーク");
        FormatPanelHint.Text = T(
            "The panel can be collapsed with Format or the › button.",
            "面板可通过顶部“格式”或 › 按钮收起。",
            "パネルは上部の「書式」または › ボタンで折りたためます。");

        if (_document is not null)
            MapStatusText.Text = $"{_document.EnumerateDepthFirst().Count()} {T("topics · autosaved", "个主题 · 已自动保存", "トピック · 自動保存済み")}";
    }

    private static int GetDepth(MindMapDocument document, MindNode node)
    {
        var depth = 0;
        var current = node;
        while (current.ParentId is Guid parentId)
        {
            depth++;
            current = document.GetNode(parentId);
        }
        return depth;
    }

    private static Brush ResourceBrush(string key, Windows.UI.Color fallback)
    {
        return ThemeService.GetBrush(key, fallback);
    }

    private static string T(string en, string zh, string ja)
    {
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh
            : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? ja
            : en;
    }
}
