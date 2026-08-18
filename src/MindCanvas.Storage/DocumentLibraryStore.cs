using System.Text.Json;

namespace MindCanvas.Storage;

public static class DocumentLibraryFolderIds
{
    public const string Graduation = "system:graduation";
    public const string Research = "system:research";
    public const string Study = "system:study";

    public static IReadOnlyList<string> SystemFolders { get; } =
        [Graduation, Research, Study];
}

public sealed record CustomDocumentFolder(
    string Id,
    string Name,
    DateTimeOffset CreatedAt);

public sealed class DocumentLibraryState
{
    public List<CustomDocumentFolder> CustomFolders { get; set; } = [];
    public Dictionary<string, string> Assignments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DocumentLibraryStore(string indexPath)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string IndexPath { get; } = Path.GetFullPath(indexPath);

    public async Task<DocumentLibraryState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(IndexPath))
            return new DocumentLibraryState();

        try
        {
            var json = await File.ReadAllTextAsync(IndexPath, cancellationToken);
            var state = JsonSerializer.Deserialize<DocumentLibraryState>(json, Options) ?? new DocumentLibraryState();
            Normalize(state);
            return state;
        }
        catch (JsonException)
        {
            return new DocumentLibraryState();
        }
    }

    public async Task<CustomDocumentFolder> CreateFolderAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedName = name.Trim();
        var state = await LoadAsync(cancellationToken);
        var existing = state.CustomFolders.FirstOrDefault(folder =>
            folder.Name.Equals(normalizedName, StringComparison.CurrentCultureIgnoreCase));
        if (existing is not null)
            return existing;

        var folder = new CustomDocumentFolder(
            $"custom:{Guid.NewGuid():N}",
            normalizedName,
            DateTimeOffset.UtcNow);
        state.CustomFolders.Add(folder);
        await SaveAsync(state, cancellationToken);
        return folder;
    }

    public async Task<bool> DeleteFolderAsync(
        string folderId,
        CancellationToken cancellationToken = default)
    {
        if (DocumentLibraryFolderIds.SystemFolders.Contains(folderId, StringComparer.Ordinal))
            return false;

        var state = await LoadAsync(cancellationToken);
        var removed = state.CustomFolders.RemoveAll(folder => folder.Id == folderId) > 0;
        if (!removed)
            return false;

        foreach (var path in state.Assignments
                     .Where(pair => pair.Value == folderId)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            state.Assignments.Remove(path);
        }
        await SaveAsync(state, cancellationToken);
        return true;
    }

    public async Task AssignAsync(
        string documentPath,
        string? folderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);
        var state = await LoadAsync(cancellationToken);
        var fullPath = Path.GetFullPath(documentPath);

        if (string.IsNullOrWhiteSpace(folderId))
        {
            state.Assignments.Remove(fullPath);
        }
        else
        {
            var valid = DocumentLibraryFolderIds.SystemFolders.Contains(folderId, StringComparer.Ordinal) ||
                        state.CustomFolders.Any(folder => folder.Id == folderId);
            if (!valid)
                throw new KeyNotFoundException($"Document folder '{folderId}' does not exist.");
            state.Assignments[fullPath] = folderId;
        }

        await SaveAsync(state, cancellationToken);
    }

    public async Task<string?> GetFolderIdAsync(
        string documentPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);
        var state = await LoadAsync(cancellationToken);
        return state.Assignments.TryGetValue(Path.GetFullPath(documentPath), out var folderId)
            ? folderId
            : null;
    }

    public async Task<DocumentLibraryState> RemoveMissingDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var state = await LoadAsync(cancellationToken);
        var changed = false;
        foreach (var path in state.Assignments.Keys.ToArray())
        {
            if (File.Exists(path))
                continue;
            state.Assignments.Remove(path);
            changed = true;
        }

        if (changed)
            await SaveAsync(state, cancellationToken);
        return state;
    }

    private async Task SaveAsync(DocumentLibraryState state, CancellationToken cancellationToken)
    {
        Normalize(state);
        Directory.CreateDirectory(Path.GetDirectoryName(IndexPath)!);
        var temporary = IndexPath + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(state, Options), cancellationToken);
        File.Move(temporary, IndexPath, true);
    }

    private static void Normalize(DocumentLibraryState state)
    {
        state.CustomFolders = state.CustomFolders
            .Where(folder => !string.IsNullOrWhiteSpace(folder.Id) && !string.IsNullOrWhiteSpace(folder.Name))
            .GroupBy(folder => folder.Id, StringComparer.Ordinal)
            .Select(group => group.First() with { Name = group.First().Name.Trim() })
            .OrderBy(folder => folder.CreatedAt)
            .ToList();

        var validCustomIds = state.CustomFolders.Select(folder => folder.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var path in state.Assignments.Keys.ToArray())
        {
            var folderId = state.Assignments[path];
            if (!DocumentLibraryFolderIds.SystemFolders.Contains(folderId, StringComparer.Ordinal) &&
                !validCustomIds.Contains(folderId))
            {
                state.Assignments.Remove(path);
            }
        }
    }
}
