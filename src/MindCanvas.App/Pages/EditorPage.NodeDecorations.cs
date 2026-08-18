using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MindCanvas.Core.Documents;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    private bool _nodeDecorationsInitialized;

    public void InitializeNodeDecorations()
    {
        if (_nodeDecorationsInitialized)
            return;

        _nodeDecorationsInitialized = true;
        SelectionChanged += (_, _) => RefreshNodeDecorations();
        DocumentChanged += (_, _) => RefreshNodeDecorations();
        Loaded += (_, _) => RefreshNodeDecorations();
        RefreshNodeDecorations();
    }

    private void RefreshNodeDecorations()
    {
        if (_document is null)
            return;

        foreach (var border in MapCanvas.Children.OfType<Border>())
        {
            if (border.Tag is not Guid nodeId || !_document.Nodes.TryGetValue(nodeId, out var node) || border.Child is not Grid grid)
                continue;

            ApplyNodeDecoration(grid, node);
        }
    }

    private static string BuildNodeIndicator(MindNode node)
    {
        var parts = new List<string>(5);
        foreach (var marker in node.Markers)
        {
            var glyph = marker.ToLowerInvariant() switch
            {
                "important" => "★",
                "done" => "✓",
                "question" => "?",
                "progress" => "◷",
                "idea" => "✦",
                _ => string.Empty
            };
            if (glyph.Length > 0 && !parts.Contains(glyph, StringComparer.Ordinal))
                parts.Add(glyph);
        }

        if (node.Priority is NodePriority.High or NodePriority.Critical)
            parts.Insert(0, node.Priority == NodePriority.Critical ? "‼" : "!");
        if (!string.IsNullOrWhiteSpace(node.Notes))
            parts.Add("≡");
        if (node.Attachments.Count > 0)
            parts.Add("▤");
        if (!string.IsNullOrWhiteSpace(node.Hyperlink))
            parts.Add("↗");

        return string.Join(' ', parts.Take(5));
    }

    private static void ApplyNodeDecoration(Grid grid, MindNode node)
    {
        const string decorationTag = "MindCanvas.NodeDecoration";
        var existing = grid.Children
            .OfType<TextBlock>()
            .FirstOrDefault(text => Equals(text.Tag, decorationTag));
        var value = BuildNodeIndicator(node);

        if (value.Length == 0)
        {
            if (existing is not null)
                grid.Children.Remove(existing);
            return;
        }

        if (grid.ColumnDefinitions.Count < 3)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        existing ??= new TextBlock
        {
            Tag = decorationTag,
            FontSize = 10,
            Opacity = 0.64,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(4, 0, 9, 0),
            IsHitTestVisible = false
        };
        existing.Text = value;
        Grid.SetColumn(existing, 2);
        if (!grid.Children.Contains(existing))
            grid.Children.Add(existing);
    }
}
