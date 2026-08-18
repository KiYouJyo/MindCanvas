using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed record DroppedContentResult(
    IReadOnlyList<Guid> CreatedNodeIds,
    IReadOnlyList<Guid> AttachmentIds);

public sealed class DroppedContentService(MindCanvasImportExportService importExport)
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".svg"
    };

    public async Task<IReadOnlyList<Guid>> AddAsync(
        MindMapDocument target,
        Guid parentId,
        IEnumerable<string> items,
        bool linkOnly = false,
        CancellationToken cancellationToken = default)
    {
        target.GetNode(parentId);
        var created = new List<Guid>();

        foreach (var raw in NormalizeItems(items))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryWebUri(raw, out var uri))
            {
                var node = target.AddChild(parentId, string.IsNullOrWhiteSpace(uri.Host) ? raw : uri.Host);
                target.SetNodeHyperlink(node.Id, raw);
                target.AddNodeAttachment(node.Id, NodeAttachmentKind.Link, node.Title, raw, true);
                created.Add(node.Id);
                continue;
            }

            var fullPath = Path.GetFullPath(raw);
            if (!File.Exists(fullPath))
                continue;

            var extension = Path.GetExtension(fullPath);
            if (!linkOnly && IsStructuredMindMap(extension))
            {
                var imported = await importExport.ImportAsync(fullPath, cancellationToken);
                created.Add(CopySubtree(imported, imported.RootNodeId, target, parentId));
                continue;
            }

            var title = Path.GetFileNameWithoutExtension(fullPath);
            var nodeForFile = target.AddChild(parentId, string.IsNullOrWhiteSpace(title) ? Path.GetFileName(fullPath) : title);
            var kind = ImageExtensions.Contains(extension) ? NodeAttachmentKind.Image : NodeAttachmentKind.File;
            target.AddNodeAttachment(nodeForFile.Id, kind, Path.GetFileName(fullPath), fullPath, true);
            created.Add(nodeForFile.Id);
        }

        return created;
    }

    public async Task<DroppedContentResult> AttachAsync(
        MindMapDocument target,
        Guid nodeId,
        IEnumerable<string> items,
        CancellationToken cancellationToken = default)
    {
        target.GetNode(nodeId);
        var createdNodes = new List<Guid>();
        var attachments = new List<Guid>();

        foreach (var raw in NormalizeItems(items))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryWebUri(raw, out var uri))
            {
                var linkAttachment = target.AddNodeAttachment(
                    nodeId,
                    NodeAttachmentKind.Link,
                    string.IsNullOrWhiteSpace(uri.Host) ? raw : uri.Host,
                    raw,
                    true);
                attachments.Add(linkAttachment.Id);
                continue;
            }

            var fullPath = Path.GetFullPath(raw);
            if (!File.Exists(fullPath))
                continue;

            var extension = Path.GetExtension(fullPath);
            if (IsStructuredMindMap(extension))
            {
                var imported = await importExport.ImportAsync(fullPath, cancellationToken);
                createdNodes.Add(CopySubtree(imported, imported.RootNodeId, target, nodeId));
                continue;
            }

            var kind = ImageExtensions.Contains(extension) ? NodeAttachmentKind.Image : NodeAttachmentKind.File;
            var fileAttachment = target.AddNodeAttachment(nodeId, kind, Path.GetFileName(fullPath), fullPath, true);
            attachments.Add(fileAttachment.Id);
        }

        return new DroppedContentResult(createdNodes, attachments);
    }

    private static IEnumerable<string> NormalizeItems(IEnumerable<string> items) =>
        items.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim());

    private static bool TryWebUri(string value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) && parsed.Scheme is "http" or "https")
        {
            uri = parsed;
            return true;
        }

        uri = null!;
        return false;
    }

    private static bool IsStructuredMindMap(string extension) =>
        extension.Equals(MindCanvasFileService.Extension, StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".opml", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".mm", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".mmd", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".mermaid", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".xmind", StringComparison.OrdinalIgnoreCase);

    private static Guid CopySubtree(MindMapDocument source, Guid sourceId, MindMapDocument target, Guid targetParentId)
    {
        var sourceNode = source.GetNode(sourceId);
        var targetNode = target.AddChild(targetParentId, sourceNode.Title);
        target.SetNodeNotes(targetNode.Id, sourceNode.Notes);
        target.SetNodeHyperlink(targetNode.Id, sourceNode.Hyperlink);
        target.SetNodePriority(targetNode.Id, sourceNode.Priority);
        target.SetNodeTags(targetNode.Id, sourceNode.Tags);
        target.SetNodeMarkers(targetNode.Id, sourceNode.Markers);
        target.SetNodeCollapsed(targetNode.Id, sourceNode.IsCollapsed);
        foreach (var attachment in sourceNode.Attachments)
            target.InsertNodeAttachment(targetNode.Id, attachment with { Id = Guid.NewGuid() });
        foreach (var childId in sourceNode.ChildrenIds)
            CopySubtree(source, childId, target, targetNode.Id);
        return targetNode.Id;
    }
}
