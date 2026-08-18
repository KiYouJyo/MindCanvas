using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

public sealed class MoveNodeCommand(
    MindMapDocument document,
    Guid nodeId,
    Guid newParentId,
    int? newIndex = null) : IUndoableCommand
{
    private Guid? _oldParentId;
    private int _oldIndex;
    private int? _resolvedNewIndex;

    public string Description => "Move node";

    public void Execute()
    {
        if (_oldParentId is null)
        {
            var node = document.GetNode(nodeId);
            _oldParentId = node.ParentId ?? throw new InvalidOperationException("The root node cannot be moved.");
            _oldIndex = document.GetNode(_oldParentId.Value).ChildrenIds.IndexOf(nodeId);
        }

        document.MoveNode(nodeId, newParentId, _resolvedNewIndex ?? newIndex);
        _resolvedNewIndex = document.GetNode(newParentId).ChildrenIds.IndexOf(nodeId);
    }

    public void Undo()
    {
        if (_oldParentId is Guid oldParentId)
            document.MoveNode(nodeId, oldParentId, _oldIndex);
    }
}
