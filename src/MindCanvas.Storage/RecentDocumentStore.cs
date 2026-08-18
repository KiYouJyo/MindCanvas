using System.Text.Json;

namespace MindCanvas.Storage;

public sealed record RecentDocumentEntry(string Path, string Title, DateTimeOffset LastOpenedAt);

public sealed class RecentDocumentStore(string indexPath, int capacity = 20)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string IndexPath { get; } = Path.GetFullPath(indexPath);
    public int Capacity { get; } = Math.Max(1, capacity);

    public async Task<IReadOnlyList<RecentDocumentEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(IndexPath))
            return [];
        try
        {
            var json = await File.ReadAllTextAsync(IndexPath, cancellationToken);
            return (JsonSerializer.Deserialize<List<RecentDocumentEntry>>(json, Options) ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.Path))
                .OrderByDescending(item => item.LastOpenedAt)
                .Take(Capacity)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<RecentDocumentEntry>> RecordAsync(
        string path,
        string title,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var items = (await LoadAsync(cancellationToken)).ToList();
        items.RemoveAll(item => string.Equals(Path.GetFullPath(item.Path), fullPath, StringComparison.OrdinalIgnoreCase));
        items.Insert(0, new RecentDocumentEntry(fullPath, title, DateTimeOffset.UtcNow));
        if (items.Count > Capacity)
            items.RemoveRange(Capacity, items.Count - Capacity);
        await SaveAsync(items, cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<RecentDocumentEntry>> RemoveMissingAsync(CancellationToken cancellationToken = default)
    {
        var items = (await LoadAsync(cancellationToken)).Where(item => File.Exists(item.Path)).ToArray();
        await SaveAsync(items, cancellationToken);
        return items;
    }

    private async Task SaveAsync(IEnumerable<RecentDocumentEntry> entries, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(IndexPath)!);
        var temporary = IndexPath + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(entries, Options), cancellationToken);
        File.Move(temporary, IndexPath, true);
    }
}
