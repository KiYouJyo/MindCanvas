using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed record RecoverableSnapshot(
    Guid DocumentId,
    string Title,
    string Path,
    DateTimeOffset SavedAt,
    MindMapDocument Document);

public sealed class AutosaveRecoveryService(
    MindCanvasFileService fileService,
    string autosaveDirectory)
{
    public string AutosaveDirectory { get; } = Path.GetFullPath(autosaveDirectory);

    public async Task<string> SaveSnapshotAsync(MindMapDocument document, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(AutosaveDirectory);
        var path = GetSnapshotPath(document.Id);
        await fileService.SaveAsync(document, path, cancellationToken);
        return path;
    }

    public async Task<MindMapDocument?> TryLoadSnapshotAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var path = GetSnapshotPath(documentId);
        return File.Exists(path) ? await fileService.LoadAsync(path, cancellationToken) : null;
    }

    public async Task<IReadOnlyList<RecoverableSnapshot>> GetRecoverableSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(AutosaveDirectory))
            return [];

        var snapshots = new List<RecoverableSnapshot>();
        foreach (var path in Directory.EnumerateFiles(AutosaveDirectory, $"*{MindCanvasFileService.Extension}"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var document = await fileService.LoadAsync(path, cancellationToken);
                snapshots.Add(new RecoverableSnapshot(
                    document.Id,
                    document.Title,
                    path,
                    File.GetLastWriteTimeUtc(path),
                    document));
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or NotSupportedException)
            {
                // Ignore corrupt or incompatible autosave files; the caller can still recover other documents.
            }
        }

        return snapshots.OrderByDescending(snapshot => snapshot.SavedAt).ToArray();
    }

    public bool DeleteSnapshot(Guid documentId)
    {
        var path = GetSnapshotPath(documentId);
        if (!File.Exists(path))
            return false;
        File.Delete(path);
        return true;
    }

    public string GetSnapshotPath(Guid documentId) =>
        Path.Combine(AutosaveDirectory, $"{documentId:N}{MindCanvasFileService.Extension}");
}
