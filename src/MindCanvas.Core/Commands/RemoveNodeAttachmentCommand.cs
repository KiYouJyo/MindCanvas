using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

public sealed class RemoveNodeAttachmentCommand(
    MindMapDocument document,
    Guid nodeId,
    Guid attachmentId) : IUndoableCommand
{
    private NodeAttachment? _removed;
    private int _index = -1;

    public string Description => "Remove attachment";

    public void Execute()
    {
        var node = document.GetNode(nodeId);
        if (_removed is null)
        {
            _index = node.Attachments.FindIndex(item => item.Id == attachmentId);
            if (_index < 0)
                return;
            _removed = node.Attachments[_index];
        }

        document.RemoveNodeAttachment(nodeId, attachmentId);
    }

    public void Undo()
    {
        if (_removed is null)
            return;

        var node = document.GetNode(nodeId);
        if (node.Attachments.Any(item => item.Id == _removed.Id))
            return;

        var index = Math.Clamp(_index, 0, node.Attachments.Count);
        node.Attachments.Insert(index, _removed);
        document.TouchExternalMutation();
    }
}
