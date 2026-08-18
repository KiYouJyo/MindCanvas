using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

/// <summary>
/// Inserts a document-independent subtree with fresh node and attachment IDs.
/// Undo removes the inserted subtree; redo restores the same generated IDs and
/// sibling order that were produced by the first execution.
/// </summary>
public sealed class InsertSubtreeCommand(
    MindMapDocument document,
    Guid parentId,
    NodeSubtreeTemplate template,
    int? index = null) : IUndoableCommand
{
    private MindNode[]? _snapshot;
    private Guid? _createdRootId;
    private int _resolvedIndex;

    public string Description => "Insert subtree";
    public Guid? CreatedRootId => _createdRootId;

    public void Execute()
    {
        if (_snapshot is null)
        {
            _createdRootId = Insert(template, parentId, index);
            var parent = document.GetNode(parentId);
            _resolvedIndex = parent.ChildrenIds.IndexOf(_createdRootId.Value);
            _snapshot = CaptureInserted(_createdRootId.Value).ToArray();
            return;
        }

        if (_createdRootId is not Guid rootId)
            throw new InvalidOperationException("Inserted subtree root is unavailable.");

        document.RestoreSubtree(_snapshot);
        var restoredParent = document.GetNode(parentId);
        restoredParent.ChildrenIds.Remove(rootId);
        restoredParent.ChildrenIds.Insert(
            Math.Clamp(_resolvedIndex, 0, restoredParent.ChildrenIds.Count),
            rootId);
    }

    public void Undo()
    {
        if (_createdRootId is Guid rootId && document.Nodes.ContainsKey(rootId))
            document.RemoveSubtree(rootId);
    }

    private Guid Insert(NodeSubtreeTemplate source, Guid targetParentId, int? targetIndex)
    {
        var node = document.AddChild(targetParentId, source.Title, targetIndex);
        document.SetNodeNotes(node.Id, source.Notes);
        document.SetNodeHyperlink(node.Id, source.Hyperlink);
        document.SetNodePriority(node.Id, source.Priority);
        document.SetNodeTags(node.Id, source.Tags);
        document.SetNodeMarkers(node.Id, source.Markers);
        foreach (var attachment in source.Attachments)
        {
            document.AddNodeAttachment(
                node.Id,
                attachment.Kind,
                attachment.Name,
                attachment.Target,
                attachment.IsLinked);
        }
        document.SetNodeCollapsed(node.Id, source.IsCollapsed);

        foreach (var child in source.Children)
            Insert(child, node.Id, null);

        return node.Id;
    }

    private IEnumerable<MindNode> CaptureInserted(Guid nodeId)
    {
        var node = document.GetNode(nodeId);
        yield return node.Clone();
        foreach (var childId in node.ChildrenIds)
        {
            foreach (var child in CaptureInserted(childId))
                yield return child;
        }
    }
}
