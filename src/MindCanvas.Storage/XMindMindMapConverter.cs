using System.IO.Compression;
using System.Text;
using System.Text.Json;
using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed class XMindMindMapConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task ExportAsync(MindMapDocument document, string path, CancellationToken cancellationToken = default)
    {
        document.Validate();
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + ".tmp";
        if (File.Exists(temporary)) File.Delete(temporary);

        await using (var stream = File.Create(temporary))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
        {
            var sheet = new XMindSheet(
                Guid.NewGuid().ToString("N"),
                "sheet",
                document.Title,
                BuildTopic(document.RootNodeId));
            await WriteJsonEntryAsync(archive, "content.json", new[] { sheet }, cancellationToken);
            await WriteJsonEntryAsync(archive, "metadata.json", new { creator = new { name = "MindCanvas" } }, cancellationToken);
            await WriteJsonEntryAsync(archive, "manifest.json", new { fileEntries = new Dictionary<string, object> { ["content.json"] = new { }, ["metadata.json"] = new { } } }, cancellationToken);
        }
        File.Move(temporary, fullPath, true);

        XMindTopic BuildTopic(Guid nodeId)
        {
            var node = document.GetNode(nodeId);
            XMindChildren? children = node.ChildrenIds.Count == 0
                ? null
                : new XMindChildren(node.ChildrenIds.Select(BuildTopic).ToArray());
            return new XMindTopic(
                node.Id.ToString("N"),
                "topic",
                node.Title,
                children,
                string.IsNullOrWhiteSpace(node.Notes) ? null : new XMindNotes(new XMindPlainNotes(node.Notes!)),
                node.Hyperlink);
        }
    }

    public async Task<MindMapDocument> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var entry = archive.GetEntry("content.json") ?? throw new InvalidDataException("The XMind package has no content.json entry.");
        await using var content = entry.Open();
        var sheets = await JsonSerializer.DeserializeAsync<XMindSheet[]>(content, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("The XMind content is empty.");
        var sheet = sheets.FirstOrDefault(item => item.RootTopic is not null)
            ?? throw new InvalidDataException("The XMind package has no root topic.");
        var rootTopic = sheet.RootTopic!;
        var document = MindMapDocument.Create(string.IsNullOrWhiteSpace(rootTopic.Title) ? sheet.Title ?? "Imported XMind" : rootTopic.Title);
        Apply(document.Root, rootTopic);
        ImportChildren(rootTopic, document.RootNodeId);
        document.SchemaVersion = MindMapDocument.CurrentSchemaVersion;
        return document;

        void ImportChildren(XMindTopic source, Guid parentId)
        {
            foreach (var childTopic in source.Children?.Attached ?? [])
            {
                var child = document.AddChild(parentId, string.IsNullOrWhiteSpace(childTopic.Title) ? "Untitled" : childTopic.Title);
                Apply(child, childTopic);
                ImportChildren(childTopic, child.Id);
            }
        }
    }

    private static void Apply(MindNode node, XMindTopic topic)
    {
        node.Notes = topic.Notes?.Plain?.Content;
        node.Hyperlink = topic.Href;
    }

    private static async Task WriteJsonEntryAsync<T>(ZipArchive archive, string name, T value, CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private sealed record XMindSheet(string Id, string Class, string? Title, XMindTopic? RootTopic);
    private sealed record XMindTopic(string Id, string Class, string Title, XMindChildren? Children, XMindNotes? Notes, string? Href);
    private sealed record XMindChildren(XMindTopic[] Attached);
    private sealed record XMindNotes(XMindPlainNotes Plain);
    private sealed record XMindPlainNotes(string Content);
}
