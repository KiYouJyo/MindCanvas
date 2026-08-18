namespace MindCanvas.Core.Documents;

public sealed record NodeAttachmentTemplate(
    NodeAttachmentKind Kind,
    string Name,
    string Target,
    bool IsLinked);

/// <summary>
/// Immutable, document-independent representation of a node subtree.
/// Node, parent, and attachment IDs are intentionally excluded so the same
/// template can be pasted safely into this document, another tab, or a future
/// document without identity collisions.
/// </summary>
public sealed record NodeSubtreeTemplate(
    string Title,
    string? Notes,
    string? Hyperlink,
    NodePriority Priority,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Markers,
    IReadOnlyList<NodeAttachmentTemplate> Attachments,
    bool IsCollapsed,
    IReadOnlyList<NodeSubtreeTemplate> Children)
{
    public NodeSubtreeTemplate(
        string title,
        string? notes,
        string? hyperlink,
        bool isCollapsed,
        IReadOnlyList<NodeSubtreeTemplate> children)
        : this(
            title,
            notes,
            hyperlink,
            NodePriority.None,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<NodeAttachmentTemplate>(),
            isCollapsed,
            children)
    {
    }

    public static NodeSubtreeTemplate Capture(MindMapDocument document, Guid nodeId)
    {
        ArgumentNullException.ThrowIfNull(document);
        var node = document.GetNode(nodeId);
        return new NodeSubtreeTemplate(
            node.Title,
            node.Notes,
            node.Hyperlink,
            node.Priority,
            [.. node.Tags],
            [.. node.Markers],
            node.Attachments
                .Select(attachment => new NodeAttachmentTemplate(
                    attachment.Kind,
                    attachment.Name,
                    attachment.Target,
                    attachment.IsLinked))
                .ToArray(),
            node.IsCollapsed,
            node.ChildrenIds.Select(childId => Capture(document, childId)).ToArray());
    }
}
