namespace MindCanvas.Storage;

public enum TrackedFileChange
{
    Unchanged,
    Modified,
    Deleted
}

public readonly record struct FileStamp(DateTime LastWriteTimeUtc, long Length);

public sealed class FileChangeTracker
{
    private readonly Dictionary<string, FileStamp> _accepted = new(StringComparer.OrdinalIgnoreCase);

    public void Accept(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            _accepted.Remove(fullPath);
            return;
        }
        _accepted[fullPath] = ReadStamp(fullPath);
    }

    public void Forget(string path)
        => _accepted.Remove(Path.GetFullPath(path));

    public TrackedFileChange GetStatus(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!_accepted.TryGetValue(fullPath, out var accepted))
        {
            if (File.Exists(fullPath))
                _accepted[fullPath] = ReadStamp(fullPath);
            return TrackedFileChange.Unchanged;
        }

        if (!File.Exists(fullPath))
            return TrackedFileChange.Deleted;

        return ReadStamp(fullPath) == accepted
            ? TrackedFileChange.Unchanged
            : TrackedFileChange.Modified;
    }

    private static FileStamp ReadStamp(string path)
    {
        var info = new FileInfo(path);
        return new FileStamp(info.LastWriteTimeUtc, info.Length);
    }
}
