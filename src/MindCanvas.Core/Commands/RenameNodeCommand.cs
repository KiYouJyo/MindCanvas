using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

public sealed class RenameNodeCommand(MindMapDocument document, Guid nodeId, string newTitle) : IUndoableCommand
{
    private string? _oldTitle;
    public string Description => "Rename node";

    public void Execute()
    {
        _oldTitle ??= document.GetNode(nodeId).Title;
        document.RenameNode(nodeId, newTitle);
    }

    public void Undo()
    {
        if (_oldTitle is not null)
            document.RenameNode(nodeId, _oldTitle);
    }
}
