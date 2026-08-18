using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public enum MindCanvasExchangeFormat
{
    Native,
    Markdown,
    Opml
}

public sealed class MindCanvasImportExportService(
    MindCanvasFileService fileService,
    MarkdownMindMapConverter markdown,
    OpmlMindMapConverter opml)
{
    public async Task<MindMapDocument> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return DetectFormat(path) switch
        {
            MindCanvasExchangeFormat.Native => await fileService.LoadAsync(path, cancellationToken),
            MindCanvasExchangeFormat.Markdown => markdown.Import(await File.ReadAllTextAsync(path, cancellationToken), Path.GetFileNameWithoutExtension(path)),
            MindCanvasExchangeFormat.Opml => opml.Import(await File.ReadAllTextAsync(path, cancellationToken), Path.GetFileNameWithoutExtension(path)),
            _ => throw new NotSupportedException($"Unsupported import format: {Path.GetExtension(path)}")
        };
    }

    public async Task ExportAsync(
        MindMapDocument document,
        string path,
        MindCanvasExchangeFormat? format = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var selected = format ?? DetectFormat(path);
        switch (selected)
        {
            case MindCanvasExchangeFormat.Native:
                await fileService.SaveAsync(document, path, cancellationToken);
                break;
            case MindCanvasExchangeFormat.Markdown:
                await WriteTextAtomicallyAsync(path, markdown.Export(document), cancellationToken);
                break;
            case MindCanvasExchangeFormat.Opml:
                await WriteTextAtomicallyAsync(path, opml.Export(document), cancellationToken);
                break;
            default:
                throw new NotSupportedException($"Unsupported export format: {selected}");
        }
    }

    public static MindCanvasExchangeFormat DetectFormat(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            MindCanvasFileService.Extension => MindCanvasExchangeFormat.Native,
            ".md" or ".markdown" => MindCanvasExchangeFormat.Markdown,
            ".opml" => MindCanvasExchangeFormat.Opml,
            _ => throw new NotSupportedException($"Unsupported MindCanvas exchange format: {Path.GetExtension(path)}")
        };

    private static async Task WriteTextAtomicallyAsync(string path, string content, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + ".tmp";
        await File.WriteAllTextAsync(temporary, content, cancellationToken);
        File.Move(temporary, fullPath, true);
    }
}
