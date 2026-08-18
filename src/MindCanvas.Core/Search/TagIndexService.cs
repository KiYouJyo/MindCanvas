namespace MindCanvas.Core.Search;

public sealed record TagSummary(string Name, int NodeCount, int DocumentCount);

public sealed class TagIndexService
{
    public IReadOnlyList<TagSummary> Build(IEnumerable<DocumentSearchSource> sources)
    {
        var index = new Dictionary<string, TagBucket>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            foreach (var node in source.Document.EnumerateDepthFirst())
            {
                foreach (var tag in node.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!index.TryGetValue(tag, out var bucket))
                    {
                        bucket = new TagBucket(tag);
                        index[tag] = bucket;
                    }
                    bucket.NodeCount++;
                    bucket.DocumentIds.Add(source.Document.Id);
                }
            }
        }

        return index.Values
            .Select(bucket => new TagSummary(bucket.Name, bucket.NodeCount, bucket.DocumentIds.Count))
            .OrderByDescending(item => item.NodeCount)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private sealed class TagBucket(string name)
    {
        public string Name { get; } = name;
        public int NodeCount { get; set; }
        public HashSet<Guid> DocumentIds { get; } = [];
    }
}
