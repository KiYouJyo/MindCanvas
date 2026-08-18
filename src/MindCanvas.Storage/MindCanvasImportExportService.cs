using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public enum MindCanvasExchangeFormat
{
    Native,
    Markdown,
    Opml,
    FreeMind,
    Mermaid,
    XMind
}

public sealed class MindCanvasImportExportService
{
    private readonly MindCanvasFileService _fileService;
    private readonly MarkdownMindMapConverter _markdown;
    private readonly OpmlMindMapConverter _opml;
    private readonly FreeMindMindMapConverter _freeMind;
    private readonly MermaidMindMapConverter _mermaid;
    private readonly XMindMindMapConverter _xmind;

    public MindCanvasImportExportService(
        MindCanvasFileService fileService,
        MarkdownMindMapConverter markdown,
        OpmlMindMapConverter opml)
        : this(fileService, markdown, opml, new FreeMindMindMapConverter(), new MermaidMindMapConverter(), new XMindMindMapConverter())
    {
    }

    public MindCanvasImportExportService(
        MindCanvasFileService fileService,
        MarkdownMindMapConverter markdown,
        OpmlMindMapConverter opml,
        FreeMindMindMapConverter freeMind,
        MermaidMindMapConverter mermaid,
        XMindMindMapConverter xmind)
    {
        _fileService = fileService;
        _markdown = markdown;
        _opml = opml;
        _freeMind = freeMind;
        _mermaid = mermaid;
        _xmind = xmind;
    }

    public async Task<MindMapDocument> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return DetectFormat(path) switch
        {
            MindCanvasExchangeFormat.Native => await _fileService.LoadAsync(path, cancellationToken),
            MindCanvasExchangeFormat.Markdown => _markdown.Import(await File.ReadAllTextAsync(path, cancellationToken), Path.GetFileNameWithoutExtension(path)),
            MindCanvasExchangeFormat.Opml => _opml.Import(await File.ReadAllTextAsync(path, cancellationToken), Path.GetFileNameWithoutExtension(path)),
            MindCanvasExchangeFormat.FreeMind => _freeMind.Import(await File.ReadAllTextAsync(path, cancellationToken), Path.GetFileNameWithoutExtension(path)),
            MindCanvasExchangeFormat.Mermaid => _mermaid.Import(await File.ReadAllTextAsync(path, cancellationToken), Path.GetFileNameWithoutExtension(path)),
            MindCanvasExchangeFormat.XMind => await _xmind.ImportAsync(path, cancellationToken),
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
                await _fileService.SaveAsync(document, path, cancellationToken);
                break;
            case MindCanvasExchangeFormat.Markdown:
                await WriteTextAtomicallyAsync(path, _markdown.Export(document), cancellationToken);
                break;
            case MindCanvasExchangeFormat.Opml:
                await WriteTextAtomicallyAsync(path, _opml.Export(document), cancellationToken);
                break;
            case MindCanvasExchangeFormat.FreeMind:
                await WriteTextAtomicallyAsync(path, _freeMind.Export(document), cancellationToken);
                break;
            case MindCanvasExchangeFormat.Mermaid:
                await WriteTextAtomicallyAsync(path, _mermaid.Export(document), cancellationToken);
                break;
            case MindCanvasExchangeFormat.XMind:
                await _xmind.ExportAsync(document, path, cancellationToken);
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
            ".mm" => MindCanvasExchangeFormat.FreeMind,
            ".mmd" or ".mermaid" => MindCanvasExchangeFormat.Mermaid,
            ".xmind" => MindCanvasExchangeFormat.XMind,
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
