using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Core.Tests;

public sealed class AttachmentCommandTests
{
    [Fact]
    public void Removing_attachment_is_undoable_and_preserves_identity_and_order()
    {
        var document = MindMapDocument.Create("Root");
        var node = document.AddChild(document.RootNodeId, "Node");
        var first = document.AddNodeAttachment(node.Id, NodeAttachmentKind.File, "first.pdf", "C:/first.pdf");
        var second = document.AddNodeAttachment(node.Id, NodeAttachmentKind.Image, "second.png", "C:/second.png");
        var history = new UndoRedoManager();
        var revisionBefore = document.Revision;

        history.Execute(new RemoveNodeAttachmentCommand(document, node.Id, first.Id));

        Assert.Single(document.GetNode(node.Id).Attachments);
        Assert.Equal(second.Id, document.GetNode(node.Id).Attachments[0].Id);
        Assert.True(document.Revision > revisionBefore);

        Assert.True(history.Undo());
        Assert.Equal([first.Id, second.Id], document.GetNode(node.Id).Attachments.Select(item => item.Id).ToArray());

        Assert.True(history.Redo());
        Assert.Equal([second.Id], document.GetNode(node.Id).Attachments.Select(item => item.Id).ToArray());
    }
}
