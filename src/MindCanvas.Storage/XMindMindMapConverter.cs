using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
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
        if (archive.GetEntry("content.json") is { } jsonEntry)
            return await ImportModernAsync(jsonEntry, cancellationToken);
        if (archive.GetEntry("content.xml") is { } xmlEntry)
            return await ImportLegacyAsync(xmlEntry, cancellationToken);
        throw new InvalidDataException("The XMind package contains neither content.json nor legacy content.xml.");
    }

    private static async Task<MindMapDocument> ImportModernAsync(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        await using var content = entry.Open();
        var sheets = await JsonSerializer.DeserializeAsync<XMindSheet[]>(content, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("The XMind content is empty.");
        var sheet = sheets.FirstOrDefault(item => item.RootTopic is not null)
            ?? throw new InvalidDataException("The XMind package has no root topic.");
        var rootTopic = sheet.RootTopic!;
        var document = MindMapDocument.Create(string.IsNullOrWhiteSpace(rootTopic.Title) ? sheet.Title ?? "Imported XMind" : rootTopic.Title);
        Apply(document.Root, rootTopic);
        ImportChildren(rootTopic, document, document.RootNodeId);
        document.SchemaVersion = MindMapDocument.CurrentSchemaVersion;
        return document;
    }

    private static async Task<MindMapDocument> ImportLegacyAsync(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        await using var content = entry.Open();
        var xml = await XDocument.LoadAsync(content, LoadOptions.None, cancellationToken);
        var sheet = xml.Descendants().FirstOrDefault(element => element.Name.LocalName == "sheet")
            ?? throw new InvalidDataException("The legacy XMind package has no sheet.");
        var rootTopic = sheet.Elements().FirstOrDefault(element => element.Name.LocalName == "topic")
            ?? sheet.Descendants().FirstOrDefault(element => element.Name.LocalName == "topic")
            ?? throw new InvalidDataException("The legacy XMind sheet has no root topic.");
        var sheetTitle = ChildValue(sheet, "title");
        var rootTitle = ChildValue(rootTopic, "title");
        var document = MindMapDocument.Create(string.IsNullOrWhiteSpace(rootTitle) ? sheetTitle ?? "Imported XMind" : rootTitle!);
        ApplyLegacy(document.Root, rootTopic);
        ImportLegacyChildren(rootTopic, document, document.RootNodeId);
        document.SchemaVersion = MindMapDocument.CurrentSchemaVersion;
        return document;
    }

    private static void ImportChildren(XMindTopic source, MindMapDocument document, Guid parentId)
    {
        foreach (var childTopic in source.Children?.Attached ?? [])
        {
            var child = document.AddChild(parentId, string.IsNullOrWhiteSpace(childTopic.Title) ? "Untitled" : childTopic.Title);
            Apply(child, childTopic);
            ImportChildren(childTopic, document, child.Id);
        }
    }

    private static void ImportLegacyChildren(XElement source, MindMapDocument document, Guid parentId)
    {
        var childrenContainer = source.Elements().FirstOrDefault(element => element.Name.LocalName == "children");
        if (childrenContainer is null)
            return;

        var topicsGroups = childrenContainer.Elements().Where(element => element.Name.LocalName == "topics");
        foreach (var group in topicsGroups)
        {
            var type = group.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "type")?.Value;
            if (!string.IsNullOrWhiteSpace(type) && !type.Equals("attached", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var childTopic in group.Elements().Where(element => element.Name.LocalName == "topic"))
            {
                var title = ChildValue(childTopic, "title");
                var child = document.AddChild(parentId, string.IsNullOrWhiteSpace(title) ? "Untitled" : title!);
                ApplyLegacy(child, childTopic);
                ImportLegacyChildren(childTopic, document, child.Id);
            }
        }
    }

    private static void Apply(MindNode node, XMindTopic topic)
    {
        node.Notes = topic.Notes?.Plain?.Content;
        node.Hyperlink = topic.Href;
    }

    private static void ApplyLegacy(MindNode node, XElement topic)
    {
        var notes = topic.Elements().FirstOrDefault(element => element.Name.LocalName == "notes");
        node.Notes = notes?.Descendants().FirstOrDefault(element => element.Name.LocalName == "plain")?.Value?.Trim();
        node.Hyperlink = topic.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "href")?.Value;
    }

    private static string? ChildValue(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(element => element.Name.LocalName == localName)?.Value?.Trim();

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
