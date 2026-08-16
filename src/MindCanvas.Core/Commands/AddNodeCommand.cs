using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

public sealed class AddNodeCommand(
    MindMapDocument document,
    Guid parentId,
    string title,
    int? index = null) : IUndoableCommand
{
    private Guid? _nodeId;
    private MindNode? _snapshot;
    private int? _resolvedIndex;

    public string Description => "Add node";
    public Guid? CreatedNodeId => _nodeId;

    public void Execute()
    {
        if (_snapshot is null)
        {
            var node = document.AddChild(parentId, title, index);
            _nodeId = node.Id;
            _snapshot = node.Clone();
            _resolvedIndex = document.GetNode(parentId).ChildrenIds.IndexOf(node.Id);
            return;
        }

        var restored = _snapshot.Clone();
        document.Nodes[restored.Id] = restored;
        var parent = document.GetNode(parentId);
        parent.ChildrenIds.Remove(restored.Id);
        var targetIndex = Math.Clamp(_resolvedIndex ?? parent.ChildrenIds.Count, 0, parent.ChildrenIds.Count);
        parent.ChildrenIds.Insert(targetIndex, restored.Id);
        _nodeId = restored.Id;
    }

    public void Undo()
    {
        if (_nodeId is Guid id && document.Nodes.ContainsKey(id))
            document.RemoveSubtree(id);
    }
}
