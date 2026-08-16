namespace MindCanvas.Core.Documents;

/// <summary>
/// Immutable, document-independent representation of a node subtree.
/// It intentionally excludes node IDs and parent IDs so the same template can
/// be pasted into the current document, another tab, or a future document.
/// </summary>
public sealed record NodeSubtreeTemplate(
    string Title,
    string? Notes,
    string? Hyperlink,
    bool IsCollapsed,
    IReadOnlyList<NodeSubtreeTemplate> Children)
{
    public static NodeSubtreeTemplate Capture(MindMapDocument document, Guid nodeId)
    {
        ArgumentNullException.ThrowIfNull(document);
        var node = document.GetNode(nodeId);
        return new NodeSubtreeTemplate(
            node.Title,
            node.Notes,
            node.Hyperlink,
            node.IsCollapsed,
            node.ChildrenIds.Select(childId => Capture(document, childId)).ToArray());
    }
}
