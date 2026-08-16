using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using MindCanvas.Theming;
using MindCanvas.Core.Documents;
using MindCanvas.Layout;

namespace MindCanvas.Pages;

public sealed partial class EditorPage : Page
{
    private MindMapDocument? _document;
    private string _mode = "split";
    private bool _formatVisible = true;

    public EditorPage()
    {
        InitializeComponent();
        SetFormatTexts();
        SetView("split");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var navigation = e.Parameter switch
        {
            EditorNavigation nav => nav,
            MindMapDocument document => new EditorNavigation(document, "split"),
            _ => new EditorNavigation(null, "split")
        };
        _document = navigation.Document;
        SetView(navigation.Mode);
    }

    public void SetView(string mode)
    {
        _mode = mode is "map" or "outline" or "split" ? mode : "split";

        if (_mode == "outline")
        {
            OutlineColumn.Width = new GridLength(0);
            NarrowOutlinePane.Visibility = Visibility.Collapsed;
            WideOutlinePane.Visibility = Visibility.Visible;
            MapHost.Visibility = Visibility.Collapsed;
        }
        else
        {
            OutlineColumn.Width = new GridLength(306);
            NarrowOutlinePane.Visibility = Visibility.Visible;
            WideOutlinePane.Visibility = Visibility.Collapsed;
            MapHost.Visibility = Visibility.Visible;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (_document is null) return;
        RenderMap(_document);
        PopulateOutline(_document);
        PopulateWideOutline(_document);
        SetFormatTexts();
    }

    public void ToggleFormatPanel()
    {
        _formatVisible = !_formatVisible;
        FormatPanel.Visibility = _formatVisible ? Visibility.Visible : Visibility.Collapsed;
        FormatColumn.Width = _formatVisible ? new GridLength(260) : new GridLength(0);
    }

    public void ToggleCollapse(bool collapse)
    {
        // Collapse/expand is represented in the V4 toolbar, but intentionally does not mutate
        // the v0.1.0 document model in this UI-only change.
        if (_document is not null)
            PopulateOutline(_document);
    }

    private void CollapseFormatButton_Click(object sender, RoutedEventArgs e) => ToggleFormatPanel();

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
                StrokeThickness = 1.5
            };
            MapCanvas.Children.Add(line);
        }

        foreach (var layout in snapshot.Nodes.Values)
        {
            var node = document.GetNode(layout.NodeId);
            var border = new Border
            {
                Width = layout.Bounds.Width,
                Height = layout.Bounds.Height,
                CornerRadius = new CornerRadius(6),
                Background = ResourceBrush("V4NodeBackgroundBrush", Colors.White),
                BorderBrush = ResourceBrush("V4NodeStrokeBrush", ColorHelper.FromArgb(255, 235, 235, 235)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var accent = new Border
            {
                Background = ResourceBrush("V4NodeAccentBlueBrush", ColorHelper.FromArgb(255, 8, 107, 194)),
                CornerRadius = new CornerRadius(2, 0, 0, 2)
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
                    : Microsoft.UI.Text.FontWeights.Normal
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
                StrokeThickness = 1
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
                StrokeThickness = 1
            });
        }
    }

    private void PopulateOutline(MindMapDocument document)
    {
        OutlineTree.RootNodes.Clear();
        TreeViewNode Build(Guid id)
        {
            var node = document.GetNode(id);
            var result = new TreeViewNode { Content = node.Title, IsExpanded = true };
            foreach (var child in node.ChildrenIds)
                result.Children.Add(Build(child));
            return result;
        }
        OutlineTree.RootNodes.Add(Build(document.RootNodeId));
    }

    private void PopulateWideOutline(MindMapDocument document)
    {
        WideOutlineRows.Children.Clear();
        var index = 0;
        foreach (var node in document.EnumerateDepthFirst())
        {
            var row = new Border
            {
                Height = 54,
                CornerRadius = new CornerRadius(4),
                Background = index == 0
                    ? ResourceBrush("V4ControlSelectedBackgroundBrush", ColorHelper.FromArgb(255, 231, 241, 251))
                    : ResourceBrush("V4CardBackgroundBrush", Colors.White),
                BorderBrush = index == 0
                    ? ResourceBrush("V4ControlSelectedStrokeBrush", ColorHelper.FromArgb(255, 96, 166, 230))
                    : ResourceBrush("V4CardStrokeBrush", ColorHelper.FromArgb(255, 235, 235, 235)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(116) });

            var title = new TextBlock
            {
                Text = node.Title,
                FontSize = 14,
                Foreground = index == 0
                    ? ResourceBrush("V4AccentForegroundBrush", ColorHelper.FromArgb(255, 0, 99, 186))
                    : ResourceBrush("V4TextStrongBrush", ColorHelper.FromArgb(255, 23, 26, 31)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(34, 0, 0, 0)
            };
            grid.Children.Add(title);
            Grid.SetColumn(title, 0);

            var note = new TextBlock
            {
                Text = node.Notes ?? T("Project overview", "项目总览", "プロジェクト概要"),
                FontSize = 12,
                Foreground = ResourceBrush("V4TextSecondaryBrush", ColorHelper.FromArgb(255, 97, 105, 117)),
                VerticalAlignment = VerticalAlignment.Center
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

    private void SetFormatTexts()
    {
        OutlinePaneTitle.Text = T("Outline", "大纲", "アウトライン");
        OutlineSearch.PlaceholderText = T("Search nodes", "搜索节点", "ノードを検索");
        OutlineAddTopicButton.Content = "＋  " + T("New topic", "新主题", "新規トピック");

        WideOutlineTitle.Text = T("Outline", "大纲", "アウトライン");
        WideOutlineCount.Text = T("9 topics · 2 collapsed branches", "9 个主题 · 2 个折叠分支", "9 トピック · 2 件の折りたたみブランチ");
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
