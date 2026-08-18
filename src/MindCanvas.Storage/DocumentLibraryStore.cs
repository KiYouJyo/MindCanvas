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

public sealed record DocumentLibraryEntry(
    string Path,
    string Title,
    DateTimeOffset LastOpenedAt);

public sealed class DocumentLibraryState
{
    public List<DocumentLibraryEntry> Documents { get; set; } = [];
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

    public async Task<DocumentLibraryState> MergeRecentDocumentsAsync(
        IEnumerable<RecentDocumentEntry> recentDocuments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recentDocuments);
        var state = await LoadAsync(cancellationToken);
        var byPath = state.Documents.ToDictionary(entry => entry.Path, StringComparer.OrdinalIgnoreCase);

        foreach (var recent in recentDocuments)
        {
            if (!TryNormalizePath(recent.Path, out var fullPath))
                continue;

            if (byPath.TryGetValue(fullPath, out var existing))
            {
                byPath[fullPath] = existing with
                {
                    Title = string.IsNullOrWhiteSpace(recent.Title) ? existing.Title : recent.Title.Trim(),
                    LastOpenedAt = recent.LastOpenedAt > existing.LastOpenedAt
                        ? recent.LastOpenedAt
                        : existing.LastOpenedAt
                };
            }
            else
            {
                byPath[fullPath] = new DocumentLibraryEntry(
                    fullPath,
                    string.IsNullOrWhiteSpace(recent.Title) ? Path.GetFileNameWithoutExtension(fullPath) : recent.Title.Trim(),
                    recent.LastOpenedAt);
            }
        }

        state.Documents = byPath.Values
            .OrderByDescending(entry => entry.LastOpenedAt)
            .ThenBy(entry => entry.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        await SaveAsync(state, cancellationToken);
        return state;
    }

    public async Task<DocumentLibraryState> RecordDocumentAsync(
        string documentPath,
        string title,
        DateTimeOffset? lastOpenedAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);
        var entry = new RecentDocumentEntry(
            Path.GetFullPath(documentPath),
            title,
            lastOpenedAt ?? DateTimeOffset.UtcNow);
        return await MergeRecentDocumentsAsync([entry], cancellationToken);
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

        foreach (var entry in state.Documents.Where(entry => !File.Exists(entry.Path)).ToArray())
        {
            state.Documents.Remove(entry);
            state.Assignments.Remove(entry.Path);
            changed = true;
        }

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
        state.Documents ??= [];
        state.CustomFolders ??= [];
        state.Assignments ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var normalizedDocuments = new Dictionary<string, DocumentLibraryEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in state.Documents)
        {
            if (entry is null || !TryNormalizePath(entry.Path, out var fullPath))
                continue;

            var normalized = entry with
            {
                Path = fullPath,
                Title = string.IsNullOrWhiteSpace(entry.Title)
                    ? Path.GetFileNameWithoutExtension(fullPath)
                    : entry.Title.Trim()
            };

            if (!normalizedDocuments.TryGetValue(fullPath, out var existing) ||
                normalized.LastOpenedAt >= existing.LastOpenedAt)
            {
                normalizedDocuments[fullPath] = normalized;
            }
        }
        state.Documents = normalizedDocuments.Values
            .OrderByDescending(entry => entry.LastOpenedAt)
            .ThenBy(entry => entry.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        state.CustomFolders = state.CustomFolders
            .Where(folder => folder is not null && !string.IsNullOrWhiteSpace(folder.Id) && !string.IsNullOrWhiteSpace(folder.Name))
            .GroupBy(folder => folder.Id, StringComparer.Ordinal)
            .Select(group => group.First() with { Name = group.First().Name.Trim() })
            .OrderBy(folder => folder.CreatedAt)
            .ToList();

        var validCustomIds = state.CustomFolders.Select(folder => folder.Id).ToHashSet(StringComparer.Ordinal);
        var normalizedAssignments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in state.Assignments)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                continue;

            var folderId = pair.Value;
            if (!DocumentLibraryFolderIds.SystemFolders.Contains(folderId, StringComparer.Ordinal) &&
                !validCustomIds.Contains(folderId))
                continue;

            if (TryNormalizePath(pair.Key, out var fullPath))
                normalizedAssignments[fullPath] = folderId;
        }
        state.Assignments = normalizedAssignments;
    }

    private static bool TryNormalizePath(string? path, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            fullPath = Path.GetFullPath(path);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
