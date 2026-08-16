using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

public sealed class AddNodeCommand(MindMapDocument document, Guid parentId, string title) : IUndoableCommand
{
    private Guid? _nodeId;
    private MindNode? _snapshot;

    public string Description => "Add node";
    public Guid? CreatedNodeId => _nodeId;

    public void Execute()
    {
        if (_snapshot is null)
        {
            var node = document.AddChild(parentId, title);
            _nodeId = node.Id;
            _snapshot = node.Clone();
            return;
        }

        var restored = _snapshot.Clone();
        document.Nodes[restored.Id] = restored;
        var parent = document.GetNode(parentId);
        if (!parent.ChildrenIds.Contains(restored.Id)) parent.ChildrenIds.Add(restored.Id);
        _nodeId = restored.Id;
    }

    public void Undo()
    {
        if (_nodeId is Guid id && document.Nodes.ContainsKey(id))
            document.RemoveSubtree(id);
    }
}
