using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed class MindCanvasFileService(MindCanvasJsonSerializer serializer)
{
    public const string Extension = ".mcanvas";

    public async Task SaveAsync(MindMapDocument document, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + ".tmp";
        await File.WriteAllTextAsync(temporary, serializer.Serialize(document), cancellationToken);
        File.Move(temporary, fullPath, true);
    }

    public async Task<MindMapDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return serializer.Deserialize(json);
    }
}
