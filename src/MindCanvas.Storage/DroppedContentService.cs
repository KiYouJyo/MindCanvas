using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

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

        foreach (var raw in items.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = raw.Trim();
            if (Uri.TryCreate(item, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            {
                var node = target.AddChild(parentId, string.IsNullOrWhiteSpace(uri.Host) ? item : uri.Host);
                target.SetNodeHyperlink(node.Id, item);
                target.AddNodeAttachment(node.Id, NodeAttachmentKind.Link, node.Title, item, true);
                created.Add(node.Id);
                continue;
            }

            var fullPath = Path.GetFullPath(item);
            if (!File.Exists(fullPath))
                continue;

            var extension = Path.GetExtension(fullPath);
            if (!linkOnly && IsStructuredMindMap(extension))
            {
                var imported = await importExport.ImportAsync(fullPath, cancellationToken);
                var importedRoot = CopySubtree(imported, imported.RootNodeId, target, parentId);
                created.Add(importedRoot);
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

    private static bool IsStructuredMindMap(string extension) =>
        extension.Equals(MindCanvasFileService.Extension, StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".opml", StringComparison.OrdinalIgnoreCase);

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
            target.AddNodeAttachment(targetNode.Id, attachment.Kind, attachment.Name, attachment.Target, attachment.IsLinked);
        foreach (var childId in sourceNode.ChildrenIds)
            CopySubtree(source, childId, target, targetNode.Id);
        return targetNode.Id;
    }
}
