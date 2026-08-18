using Microsoft.UI.Xaml.Controls;
using MindCanvas.Core.Search;

namespace MindCanvas;

public sealed partial class MainWindow
{
    private readonly NodeSearchService _globalNodeSearch = new();
    private Flyout? _globalSearchFlyout;
    private ListView? _globalSearchResults;
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
    }

    private async void DocumentsSearchBox_GlobalTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_globalSearchResults is null || _globalSearchFlyout is null)
            return;

        var query = DocumentsSearchBox.Text.Trim();
        var generation = ++_globalSearchGeneration;
        if (query.Length < 2)
        {
            _globalSearchFlyout.Hide();
            _globalSearchResults.Items.Clear();
            return;
        }

        var sources = _sessions.Values
            .Select(session => new DocumentSearchSource(session.Document, session.FilePath))
            .ToList();
        var knownIds = sources.Select(source => source.Document.Id).ToHashSet();

        if (_recentDocumentStore is not null)
        {
            foreach (var recent in (await _recentDocumentStore.LoadAsync()).Take(16))
            {
                if (generation != _globalSearchGeneration)
                    return;
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
                    // A stale recent item must not break global search.
                }
            }
        }

        if (generation != _globalSearchGeneration)
            return;

        var hits = _globalNodeSearch.Search(sources, query, new NodeSearchOptions(MaxResults: 80));
        _globalSearchResults.Items.Clear();
        foreach (var hit in hits)
        {
            var item = new ListViewItem { Tag = hit };
            var panel = new StackPanel { Spacing = 2, Padding = new Microsoft.UI.Xaml.Thickness(4) };
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
                TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis,
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
