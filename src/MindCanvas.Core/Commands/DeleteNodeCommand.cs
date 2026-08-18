using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

public sealed class DeleteNodeCommand(MindMapDocument document, Guid nodeId) : IUndoableCommand
{
    private MindNode[]? _snapshot;
    private Guid? _parentId;
    private int _parentIndex;

    public string Description => "Delete node";

    public void Execute()
    {
        if (nodeId == document.RootNodeId)
            throw new InvalidOperationException("The root node cannot be deleted.");

        if (_snapshot is null)
        {
            var node = document.GetNode(nodeId);
            _parentId = node.ParentId ?? throw new InvalidOperationException("A non-root node must have a parent.");
            _parentIndex = document.GetNode(_parentId.Value).ChildrenIds.IndexOf(nodeId);
            _snapshot = document.RemoveSubtree(nodeId).Select(n => n.Clone()).ToArray();
            return;
        }

        document.RemoveSubtree(nodeId);
    }

    public void Undo()
    {
        if (_snapshot is null || _parentId is not Guid parentId)
            return;

        document.RestoreSubtree(_snapshot);
        var parent = document.GetNode(parentId);
        parent.ChildrenIds.Remove(nodeId);
        parent.ChildrenIds.Insert(Math.Clamp(_parentIndex, 0, parent.ChildrenIds.Count), nodeId);
    }
}
