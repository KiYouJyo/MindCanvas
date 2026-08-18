using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Search;

public enum NodeSearchField
{
    Title,
    Notes,
    Tag
}

public sealed record DocumentSearchSource(MindMapDocument Document, string? SourcePath = null);

public sealed record NodeSearchHit(
    Guid DocumentId,
    string DocumentTitle,
    string? SourcePath,
    Guid NodeId,
    string NodeTitle,
    NodeSearchField Field,
    string MatchText);

public sealed record NodeSearchOptions(
    bool IncludeTitles = true,
    bool IncludeNotes = true,
    bool IncludeTags = true,
    IReadOnlyCollection<string>? RequiredTags = null,
    int MaxResults = 200);

public sealed class NodeSearchService
{
    public IReadOnlyList<NodeSearchHit> Search(
        MindMapDocument document,
        string query,
        NodeSearchOptions? options = null) =>
        Search([new DocumentSearchSource(document)], query, options);

    public IReadOnlyList<NodeSearchHit> Search(
        IEnumerable<DocumentSearchSource> sources,
        string query,
        NodeSearchOptions? options = null)
    {
        options ??= new NodeSearchOptions();
        query = query?.Trim() ?? string.Empty;
        var requiredTags = (options.RequiredTags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if ((query.Length == 0 && requiredTags.Length == 0) || options.MaxResults <= 0)
            return [];

        var results = new List<NodeSearchHit>();
        foreach (var source in sources)
        {
            var document = source.Document;
            document.Validate();
            foreach (var node in document.EnumerateDepthFirst())
            {
                if (requiredTags.Length > 0 &&
                    requiredTags.Any(tag => !node.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                    continue;

                if (query.Length == 0)
                {
                    Add(NodeSearchField.Tag, string.Join(", ", requiredTags));
                }
                else
                {
                    if (options.IncludeTitles && Contains(node.Title, query))
                        Add(NodeSearchField.Title, node.Title);

                    if (options.IncludeNotes && !string.IsNullOrWhiteSpace(node.Notes) && Contains(node.Notes, query))
                        Add(NodeSearchField.Notes, Snippet(node.Notes!, query));

                    if (options.IncludeTags)
                    {
                        foreach (var tag in node.Tags.Where(tag => Contains(tag, query)))
                            Add(NodeSearchField.Tag, tag);
                    }
                }

                if (results.Count >= options.MaxResults)
                    return results;

                void Add(NodeSearchField field, string matchText)
                {
                    if (results.Count >= options.MaxResults)
                        return;
                    results.Add(new NodeSearchHit(
                        document.Id,
                        document.Title,
                        source.SourcePath,
                        node.Id,
                        node.Title,
                        field,
                        matchText));
                }
            }
        }

        return results;
    }

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    private static string Snippet(string text, string query)
    {
        const int radius = 42;
        var index = text.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
        if (index < 0 || text.Length <= radius * 2)
            return text;
        var start = Math.Max(0, index - radius);
        var end = Math.Min(text.Length, index + query.Length + radius);
        return $"{(start > 0 ? "…" : string.Empty)}{text[start..end]}{(end < text.Length ? "…" : string.Empty)}";
    }
}
