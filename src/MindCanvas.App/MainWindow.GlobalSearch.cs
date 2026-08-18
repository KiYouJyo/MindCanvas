using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MindCanvas.Core.Search;

namespace MindCanvas;

public sealed partial class MainWindow
{
    private readonly NodeSearchService _globalNodeSearch = new();
    private readonly TagIndexService _globalTagIndex = new();
    private Flyout? _globalSearchFlyout;
    private ListView? _globalSearchResults;
    private Button? _tagsOverviewButton;
    private Flyout? _tagsOverviewFlyout;
    private ListView? _tagsOverviewList;
    private int _globalSearchGeneration;

    public void InitializeGlobalSearch()
    {
        if (_globalSearchFlyout is not null)
            return;

        _globalSearchResults = new ListView
        {
            Width = 520,
            MaxHeight = 420,
            SelectionMode = ListViewSelectionMode.Single
        };
        _globalSearchResults.SelectionChanged += GlobalSearchResults_SelectionChanged;
        _globalSearchFlyout = new Flyout { Content = _globalSearchResults };
        DocumentsSearchBox.TextChanged += DocumentsSearchBox_GlobalTextChanged;

        _tagsOverviewList = new ListView
        {
            Width = 340,
            MaxHeight = 420,
            SelectionMode = ListViewSelectionMode.Single
        };
        _tagsOverviewList.SelectionChanged += TagsOverviewList_SelectionChanged;
        _tagsOverviewFlyout = new Flyout { Content = _tagsOverviewList };
        _tagsOverviewButton = new Button
        {
            Height = 32,
            MinWidth = 82,
            Content = LocalText("Tags", "标签", "タグ")
        };
        _tagsOverviewButton.Click += TagsOverviewButton_Click;

        var actions = DocumentsActions.Children.OfType<StackPanel>().FirstOrDefault();
        actions?.Children.Insert(0, _tagsOverviewButton);
    }

    private async void DocumentsSearchBox_GlobalTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_globalSearchResults is null || _globalSearchFlyout is null)
            return;

        var query = DocumentsSearchBox.Text.Trim();
        var generation = ++_globalSearchGeneration;
        var tagQuery = query.StartsWith('#');
        if (query.Length < 2 || (tagQuery && query[1..].Trim().Length == 0))
        {
            _globalSearchFlyout.Hide();
            _globalSearchResults.Items.Clear();
            return;
        }

        var sources = await LoadGlobalSearchSourcesAsync(generation);
        if (generation != _globalSearchGeneration)
            return;

        IReadOnlyList<NodeSearchHit> hits;
        if (tagQuery)
        {
            var tag = query[1..].Trim();
            hits = _globalNodeSearch.Search(
                sources,
                string.Empty,
                new NodeSearchOptions(RequiredTags: [tag], MaxResults: 80));
        }
        else
        {
            hits = _globalNodeSearch.Search(sources, query, new NodeSearchOptions(MaxResults: 80));
        }

        _globalSearchResults.Items.Clear();
        foreach (var hit in hits)
        {
            var item = new ListViewItem { Tag = hit };
            var panel = new StackPanel { Spacing = 2, Padding = new Thickness(4) };
            panel.Children.Add(new TextBlock
            {
                Text = hit.NodeTitle,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"{hit.DocumentTitle}  ·  {hit.Field}  ·  {hit.MatchText}",
                FontSize = 11,
                Opacity = 0.68,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 470
            });
            item.Content = panel;
            _globalSearchResults.Items.Add(item);
        }

        if (hits.Count > 0)
            _globalSearchFlyout.ShowAt(DocumentsSearchBox);
        else
            _globalSearchFlyout.Hide();
    }

    private async void TagsOverviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_tagsOverviewList is null || _tagsOverviewFlyout is null || _tagsOverviewButton is null)
            return;

        var generation = ++_globalSearchGeneration;
        var sources = await LoadGlobalSearchSourcesAsync(generation);
        if (generation != _globalSearchGeneration)
            return;

        var tags = _globalTagIndex.Build(sources);
        _tagsOverviewList.Items.Clear();
        foreach (var tag in tags)
        {
            var item = new ListViewItem { Tag = tag };
            var row = new Grid { Padding = new Thickness(4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock
            {
                Text = tag.Name,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            var count = new TextBlock
            {
                Text = LocalText(
                    $"{tag.NodeCount} nodes · {tag.DocumentCount} docs",
                    $"{tag.NodeCount} 个节点 · {tag.DocumentCount} 个文档",
                    $"{tag.NodeCount} ノード · {tag.DocumentCount} 文書"),
                FontSize = 11,
                Opacity = 0.65,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(count, 1);
            row.Children.Add(count);
            item.Content = row;
            _tagsOverviewList.Items.Add(item);
        }

        if (tags.Count > 0)
            _tagsOverviewFlyout.ShowAt(_tagsOverviewButton);
    }

    private void TagsOverviewList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_tagsOverviewList?.SelectedItem is not ListViewItem { Tag: TagSummary tag })
            return;

        _tagsOverviewFlyout?.Hide();
        DocumentsSearchBox.Text = $"#{tag.Name}";
        DocumentsSearchBox.Focus(FocusState.Programmatic);
    }

    private async Task<List<DocumentSearchSource>> LoadGlobalSearchSourcesAsync(int generation)
    {
        var sources = _sessions.Values
            .Select(session => new DocumentSearchSource(session.Document, session.FilePath))
            .ToList();
        var knownIds = sources.Select(source => source.Document.Id).ToHashSet();

        if (_recentDocumentStore is null)
            return sources;

        foreach (var recent in (await _recentDocumentStore.LoadAsync()).Take(16))
        {
            if (generation != _globalSearchGeneration)
                return sources;
            if (!File.Exists(recent.Path))
                continue;
            try
            {
                var document = await App.FileService.LoadAsync(recent.Path);
                if (knownIds.Add(document.Id))
                    sources.Add(new DocumentSearchSource(document, recent.Path));
            }
            catch
            {
                // A stale or incompatible recent item must not break search/tag aggregation.
            }
        }
        return sources;
    }

    private async void GlobalSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_globalSearchResults?.SelectedItem is not ListViewItem { Tag: NodeSearchHit hit })
            return;

        _globalSearchFlyout?.Hide();
        var open = _sessions.FirstOrDefault(pair => pair.Value.Document.Id == hit.DocumentId);
        if (open.Key is not null)
        {
            DocumentTabs.SelectedItem = open.Key;
        }
        else if (!string.IsNullOrWhiteSpace(hit.SourcePath) && File.Exists(hit.SourcePath))
        {
            try
            {
                var document = await App.FileService.LoadAsync(hit.SourcePath);
                AddDocument(document, hit.SourcePath);
            }
            catch
            {
                return;
            }
        }
        else
        {
            return;
        }

        Navigate("editor");
        CurrentEditor?.SelectNode(hit.NodeId);
    }
}
