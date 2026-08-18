using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using MindCanvas.Core.Search;
using MindCanvas.Layout;
using MindCanvas.Storage;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    private readonly NodeSearchService _functionalSearch = new();
    private bool _functionalFoundationInitialized;
    private TextBox? _canvasSearchBox;
    private Flyout? _searchFlyout;
    private ListView? _searchResultsList;
    private TextBox? _notesEditor;
    private TextBox? _tagsEditor;
    private TextBox? _linkEditor;
    private ComboBox? _priorityEditor;
    private ComboBox? _layoutSelector;
    private TextBlock? _breadcrumbText;
    private Button? _focusButton;
    private Border? _miniMap;
    private Canvas? _miniMapCanvas;

    private void InitializeFunctionalFoundation()
    {
        if (_functionalFoundationInitialized)
            return;

        _functionalFoundationInitialized = true;
        BuildCanvasTools();
        BuildDetailsAndLayoutPanel();
        BuildSearchFlyout();

        OutlineSearch.TextChanged += SearchBox_TextChanged;
        WideOutlineSearch.TextChanged += SearchBox_TextChanged;
        KeyDown += FunctionalFoundation_KeyDown;
        SelectionChanged += FunctionalFoundation_SelectionChanged;
        MapScrollViewer.ViewChanged += FunctionalViewportChanged;
        MapCanvas.AllowDrop = true;
        MapCanvas.DragOver += MapCanvas_DragOver;
        MapCanvas.Drop += MapCanvas_Drop;

        RefreshFunctionalPanels();
    }

    private void BuildCanvasTools()
    {
        _canvasSearchBox = new TextBox
        {
            Width = 248,
            Height = 36,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 16, 16, 0),
            PlaceholderText = T("Search nodes, notes or tags", "搜索节点、备注或标签", "ノード・ノート・タグを検索")
        };
        _canvasSearchBox.TextChanged += SearchBox_TextChanged;
        Grid.SetRow(_canvasSearchBox, 0);
        Canvas.SetZIndex(_canvasSearchBox, 20);
        MapHost.Children.Add(_canvasSearchBox);

        var breadcrumb = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16),
            Padding = new Thickness(10, 5, 6, 5),
            CornerRadius = new CornerRadius(7),
            Background = ResourceBrush("V4ContextActionsBackgroundBrush", Microsoft.UI.Colors.White),
            BorderBrush = ResourceBrush("V4CardStrokeBrush", Microsoft.UI.Colors.LightGray),
            BorderThickness = new Thickness(1)
        };
        var breadcrumbRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        _breadcrumbText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Foreground = ResourceBrush("V4TextSecondaryBrush", Microsoft.UI.Colors.DimGray),
            MaxWidth = 430,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        _focusButton = new Button
        {
            Height = 26,
            MinWidth = 64,
            Padding = new Thickness(10, 0, 10, 0)
        };
        _focusButton.Click += FocusButton_Click;
        breadcrumbRow.Children.Add(_breadcrumbText);
        breadcrumbRow.Children.Add(_focusButton);
        breadcrumb.Child = breadcrumbRow;
        Grid.SetRow(breadcrumb, 0);
        Canvas.SetZIndex(breadcrumb, 20);
        MapHost.Children.Add(breadcrumb);

        var navigation = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 16, 50),
            Padding = new Thickness(6),
            CornerRadius = new CornerRadius(8),
            Background = ResourceBrush("V4ContextActionsBackgroundBrush", Microsoft.UI.Colors.White),
            BorderBrush = ResourceBrush("V4CardStrokeBrush", Microsoft.UI.Colors.LightGray),
            BorderThickness = new Thickness(1)
        };
        var navigationRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var centerButton = new Button
        {
            Height = 30,
            Content = T("Center selected", "定位所选", "選択へ")
        };
        centerButton.Click += (_, _) => CenterSelectedNode();
        var miniMapButton = new Button
        {
            Height = 30,
            Content = T("Mini map", "小地图", "ミニマップ")
        };
        miniMapButton.Click += (_, _) => ToggleMiniMap();
        navigationRow.Children.Add(centerButton);
        navigationRow.Children.Add(miniMapButton);
        navigation.Child = navigationRow;
        Grid.SetRow(navigation, 0);
        Canvas.SetZIndex(navigation, 20);
        MapHost.Children.Add(navigation);

        _miniMapCanvas = new Canvas { Width = 180, Height = 112 };
        _miniMap = new Border
        {
            Width = 196,
            Height = 142,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 16, 92),
            Padding = new Thickness(8, 20, 8, 8),
            CornerRadius = new CornerRadius(8),
            Background = ResourceBrush("V4ContextActionsBackgroundBrush", Microsoft.UI.Colors.White),
            BorderBrush = ResourceBrush("V4CardStrokeBrush", Microsoft.UI.Colors.LightGray),
            BorderThickness = new Thickness(1),
            Visibility = Visibility.Collapsed,
            Child = _miniMapCanvas
        };
        Grid.SetRow(_miniMap, 0);
        Canvas.SetZIndex(_miniMap, 19);
        MapHost.Children.Add(_miniMap);
    }

    private void BuildDetailsAndLayoutPanel()
    {
        if (FormatPanel.Child is not ScrollViewer scrollViewer || scrollViewer.Content is not StackPanel panel)
            return;

        panel.Children.Add(new Separator { Margin = new Thickness(0, 6, 0, 2) });
        panel.Children.Add(new TextBlock
        {
            Text = T("Layout", "布局", "レイアウト"),
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        _layoutSelector = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        _layoutSelector.Items.Add(new ComboBoxItem { Content = T("Right", "向右", "右向き"), Tag = "logic-right" });
        _layoutSelector.Items.Add(new ComboBoxItem { Content = T("Balanced", "左右", "左右"), Tag = "mindmap-balanced" });
        _layoutSelector.Items.Add(new ComboBoxItem { Content = T("Down", "向下", "下向き"), Tag = "logic-down" });
        _layoutSelector.SelectedIndex = LayoutRuntime.CurrentId switch
        {
            "mindmap-balanced" => 1,
            "logic-down" => 2,
            _ => 0
        };
        _layoutSelector.SelectionChanged += LayoutSelector_SelectionChanged;
        panel.Children.Add(_layoutSelector);

        panel.Children.Add(new Separator { Margin = new Thickness(0, 6, 0, 2) });
        panel.Children.Add(new TextBlock
        {
            Text = T("Node details", "节点详情", "ノード詳細"),
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        _priorityEditor = new ComboBox
        {
            Header = T("Priority", "优先级", "優先度"),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _priorityEditor.Items.Add(new ComboBoxItem { Content = T("None", "无", "なし"), Tag = NodePriority.None });
        _priorityEditor.Items.Add(new ComboBoxItem { Content = T("Low", "低", "低"), Tag = NodePriority.Low });
        _priorityEditor.Items.Add(new ComboBoxItem { Content = T("Medium", "中", "中"), Tag = NodePriority.Medium });
        _priorityEditor.Items.Add(new ComboBoxItem { Content = T("High", "高", "高"), Tag = NodePriority.High });
        _priorityEditor.Items.Add(new ComboBoxItem { Content = T("Critical", "紧急", "緊急"), Tag = NodePriority.Critical });
        panel.Children.Add(_priorityEditor);

        _notesEditor = new TextBox
        {
            Header = "Notes",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 110,
            MaxHeight = 220,
            PlaceholderText = T("Detailed notes for this node", "记录该节点的详细说明", "このノードの詳細ノート")
        };
        panel.Children.Add(_notesEditor);

        _tagsEditor = new TextBox
        {
            Header = T("Tags", "标签", "タグ"),
            PlaceholderText = T("Separate tags with commas", "用逗号分隔标签", "カンマでタグを区切る")
        };
        panel.Children.Add(_tagsEditor);

        _linkEditor = new TextBox
        {
            Header = T("Link", "链接", "リンク"),
            PlaceholderText = "https://"
        };
        panel.Children.Add(_linkEditor);

        var saveDetails = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = T("Save node details", "保存节点详情", "ノード詳細を保存")
        };
        saveDetails.Click += SaveNodeDetails_Click;
        panel.Children.Add(saveDetails);
    }

    private void BuildSearchFlyout()
    {
        _searchResultsList = new ListView
        {
            Width = 420,
            MaxHeight = 360,
            SelectionMode = ListViewSelectionMode.Single
        };
        _searchResultsList.SelectionChanged += SearchResultsList_SelectionChanged;
        _searchFlyout = new Flyout { Content = _searchResultsList };
    }

    private void FunctionalFoundation_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || !IsKeyDown(VirtualKey.Control) || e.Key != VirtualKey.F)
            return;

        var target = MapHost.Visibility == Visibility.Visible ? _canvasSearchBox : WideOutlineSearch;
        target?.Focus(FocusState.Programmatic);
        if (target is not null)
        {
            target.SelectionStart = 0;
            target.SelectionLength = target.Text.Length;
        }
        e.Handled = true;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_document is null || sender is not TextBox box || _searchFlyout is null || _searchResultsList is null)
            return;

        var query = box.Text.Trim();
        if (query.Length == 0)
        {
            _searchFlyout.Hide();
            return;
        }

        var hits = _functionalSearch.Search(_document, query, new NodeSearchOptions(MaxResults: 40));
        _searchResultsList.Items.Clear();
        foreach (var hit in hits)
        {
            var item = new ListViewItem { Tag = hit };
            var content = new StackPanel { Spacing = 2, Padding = new Thickness(4) };
            content.Children.Add(new TextBlock { Text = hit.NodeTitle, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            content.Children.Add(new TextBlock
            {
                Text = $"{hit.Field} · {hit.MatchText}",
                FontSize = 11,
                Opacity = 0.68,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 370
            });
            item.Content = content;
            _searchResultsList.Items.Add(item);
        }

        if (hits.Count > 0)
            _searchFlyout.ShowAt(box);
        else
            _searchFlyout.Hide();
    }

    private void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_searchResultsList?.SelectedItem is not ListViewItem { Tag: NodeSearchHit hit })
            return;
        SelectNode(hit.NodeId);
        _searchFlyout?.Hide();
        CenterSelectedNode();
    }

    private void FunctionalFoundation_SelectionChanged(object? sender, EventArgs e)
        => RefreshFunctionalPanels();

    private void RefreshFunctionalPanels()
    {
        if (_document is null || _selectedNodeId is not Guid selectedId || !_document.Nodes.TryGetValue(selectedId, out var node))
            return;

        if (LayoutRuntime.FocusRootNodeId is Guid focusId && !_document.Nodes.ContainsKey(focusId))
            LayoutRuntime.ResetFocus();

        if (_notesEditor is not null)
            _notesEditor.Text = node.Notes ?? string.Empty;
        if (_tagsEditor is not null)
            _tagsEditor.Text = string.Join(", ", node.Tags);
        if (_linkEditor is not null)
            _linkEditor.Text = node.Hyperlink ?? string.Empty;
        if (_priorityEditor is not null)
        {
            _priorityEditor.SelectedItem = _priorityEditor.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag is NodePriority priority && priority == node.Priority);
        }

        if (_breadcrumbText is not null)
            _breadcrumbText.Text = string.Join("  ›  ", DocumentProjection.GetBreadcrumb(_document, selectedId).Select(item => item.Title));
        if (_focusButton is not null)
        {
            var focused = LayoutRuntime.FocusRootNodeId is Guid;
            _focusButton.Content = focused
                ? T("Exit focus", "退出聚焦", "フォーカス解除")
                : T("Focus", "聚焦", "フォーカス");
        }
        RefreshMiniMap();
    }

    private void SaveNodeDetails_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null || _selectedNodeId is not Guid selectedId || _notesEditor is null || _tagsEditor is null || _linkEditor is null)
            return;

        var priority = (_priorityEditor?.SelectedItem as ComboBoxItem)?.Tag is NodePriority selectedPriority
            ? selectedPriority
            : NodePriority.None;
        var tags = SplitLabels(_tagsEditor.Text);
        var current = _document.GetNode(selectedId);
        if (_history is not null)
        {
            _history.Execute(new UpdateNodeDetailsCommand(
                _document,
                selectedId,
                _notesEditor.Text,
                _linkEditor.Text,
                priority,
                tags,
                current.Markers));
        }
        else
        {
            _document.SetNodeNotes(selectedId, _notesEditor.Text);
            _document.SetNodeHyperlink(selectedId, _linkEditor.Text);
            _document.SetNodePriority(selectedId, priority);
            _document.SetNodeTags(selectedId, tags);
        }
        NotifyMutation();
    }

    private static string[] SplitLabels(string value) =>
        value.Split([',', ';', '，', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private void LayoutSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_layoutSelector?.SelectedItem is not ComboBoxItem { Tag: string id })
            return;
        LayoutRuntime.CurrentId = id;
        Refresh();
        RefreshFunctionalPanels();
    }

    private void FocusButton_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null || _selectedNodeId is not Guid selectedId)
            return;

        LayoutRuntime.FocusRootNodeId = LayoutRuntime.FocusRootNodeId is Guid ? null : selectedId;
        Refresh();
        RefreshFunctionalPanels();
        FitViewButton_Click(this, new RoutedEventArgs());
    }

    private void CenterSelectedNode()
    {
        if (_selectedNodeId is not Guid selectedId)
            return;
        var element = MapCanvas.Children
            .OfType<FrameworkElement>()
            .FirstOrDefault(child => child.Tag is Guid id && id == selectedId);
        if (element is null)
            return;

        var zoom = MapScrollViewer.ZoomFactor;
        var centerX = (Canvas.GetLeft(element) + element.ActualWidth / 2) * zoom;
        var centerY = (Canvas.GetTop(element) + element.ActualHeight / 2) * zoom;
        var horizontal = Math.Max(0, centerX - MapScrollViewer.ViewportWidth / 2);
        var vertical = Math.Max(0, centerY - MapScrollViewer.ViewportHeight / 2);
        MapScrollViewer.ChangeView(horizontal, vertical, null, disableAnimation: false);
    }

    private void ToggleMiniMap()
    {
        if (_miniMap is null)
            return;
        _miniMap.Visibility = _miniMap.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        RefreshMiniMap();
    }

    private void FunctionalViewportChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        => RefreshMiniMap();

    private void RefreshMiniMap()
    {
        if (_miniMap?.Visibility != Visibility.Visible || _miniMapCanvas is null || _document is null)
            return;

        var snapshot = new RightLogicLayoutStrategy().Arrange(_document);
        _miniMapCanvas.Children.Clear();
        var scale = Math.Min(180d / Math.Max(1, snapshot.CanvasBounds.Width), 112d / Math.Max(1, snapshot.CanvasBounds.Height));
        foreach (var node in snapshot.Nodes.Values)
        {
            var marker = new Border
            {
                Width = Math.Max(2, node.Bounds.Width * scale),
                Height = Math.Max(2, node.Bounds.Height * scale),
                CornerRadius = new CornerRadius(1),
                Background = ResourceBrush("V4NodeAccentBlueBrush", Microsoft.UI.Colors.SteelBlue),
                Opacity = node.NodeId == _selectedNodeId ? 1 : 0.45
            };
            Canvas.SetLeft(marker, node.Bounds.X * scale);
            Canvas.SetTop(marker, node.Bounds.Y * scale);
            _miniMapCanvas.Children.Add(marker);
        }

        var viewport = new Border
        {
            Width = Math.Max(10, MapScrollViewer.ViewportWidth / Math.Max(0.01, MapScrollViewer.ZoomFactor) * scale),
            Height = Math.Max(8, MapScrollViewer.ViewportHeight / Math.Max(0.01, MapScrollViewer.ZoomFactor) * scale),
            BorderThickness = new Thickness(1.5),
            BorderBrush = ResourceBrush("V4AccentStrongBrush", Microsoft.UI.Colors.DodgerBlue),
            CornerRadius = new CornerRadius(2),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(viewport, MapScrollViewer.HorizontalOffset / Math.Max(0.01, MapScrollViewer.ZoomFactor) * scale);
        Canvas.SetTop(viewport, MapScrollViewer.VerticalOffset / Math.Max(0.01, MapScrollViewer.ZoomFactor) * scale);
        _miniMapCanvas.Children.Add(viewport);
    }

    private void MapCanvas_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems) ||
            e.DataView.Contains(StandardDataFormats.WebLink) ||
            e.DataView.Contains(StandardDataFormats.Text))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = T("Create nodes", "创建节点", "ノードを作成");
            e.DragUIOverride.IsCaptionVisible = true;
        }
    }

    private async void MapCanvas_Drop(object sender, DragEventArgs e)
    {
        if (_document is null)
            return;

        var values = new List<string>();
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            values.AddRange(items.OfType<StorageFile>().Select(file => file.Path));
        }
        if (e.DataView.Contains(StandardDataFormats.WebLink))
            values.Add((await e.DataView.GetWebLinkAsync()).ToString());
        else if (e.DataView.Contains(StandardDataFormats.Text))
        {
            var text = await e.DataView.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
                values.Add(text.Trim());
        }

        if (values.Count == 0)
            return;

        var exchange = new MindCanvasImportExportService(
            App.FileService,
            new MarkdownMindMapConverter(),
            new OpmlMindMapConverter());
        var drop = new DroppedContentService(exchange);
        var parentId = _selectedNodeId is Guid selected && _document.Nodes.ContainsKey(selected)
            ? selected
            : _document.RootNodeId;
        var created = await drop.AddAsync(_document, parentId, values);
        if (created.Count > 0)
        {
            _selectedNodeId = created[0];
            NotifyMutation();
        }
    }
}
