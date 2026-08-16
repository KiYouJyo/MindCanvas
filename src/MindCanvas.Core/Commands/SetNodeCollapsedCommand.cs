using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

public sealed class SetNodeCollapsedCommand(
    MindMapDocument document,
    Guid nodeId,
    bool isCollapsed) : IUndoableCommand
{
    private bool? _previousValue;

    public string Description => isCollapsed ? "Collapse node" : "Expand node";

    public void Execute()
    {
        _previousValue ??= document.GetNode(nodeId).IsCollapsed;
        document.SetNodeCollapsed(nodeId, isCollapsed);
    }

    public void Undo()
    {
        if (_previousValue is bool previous)
            document.SetNodeCollapsed(nodeId, previous);
    }
}
