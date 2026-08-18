using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Core.Tests;

public sealed class SubtreeTemplateMetadataTests
{
    [Fact]
    public void Copy_insert_preserves_metadata_but_generates_fresh_identities()
    {
        var document = MindMapDocument.Create("Root");
        var source = document.AddChild(document.RootNodeId, "Research");
        document.SetNodeNotes(source.Id, "Evidence note");
        document.SetNodeHyperlink(source.Id, "https://example.com");
        document.SetNodePriority(source.Id, NodePriority.High);
        document.SetNodeTags(source.Id, ["urban", "research"]);
        document.SetNodeMarkers(source.Id, ["important", "question"]);
        var originalAttachment = document.AddNodeAttachment(
            source.Id,
            NodeAttachmentKind.File,
            "brief.pdf",
            "C:/brief.pdf");
        var child = document.AddChild(source.Id, "Interview");
        document.SetNodeMarkers(child.Id, ["done"]);

        var template = NodeSubtreeTemplate.Capture(document, source.Id);
        var history = new UndoRedoManager();
        var command = new InsertSubtreeCommand(document, document.RootNodeId, template);
        history.Execute(command);

        var copiedId = Assert.IsType<Guid>(command.CreatedRootId);
        var copied = document.GetNode(copiedId);
        Assert.NotEqual(source.Id, copied.Id);
        Assert.Equal("Evidence note", copied.Notes);
        Assert.Equal("https://example.com", copied.Hyperlink);
        Assert.Equal(NodePriority.High, copied.Priority);
        Assert.Equal(["research", "urban"], copied.Tags.OrderBy(tag => tag).ToArray());
        Assert.Equal(["important", "question"], copied.Markers.OrderBy(marker => marker).ToArray());
        var copiedAttachment = Assert.Single(copied.Attachments);
        Assert.NotEqual(originalAttachment.Id, copiedAttachment.Id);
        Assert.Equal(originalAttachment.Kind, copiedAttachment.Kind);
        Assert.Equal(originalAttachment.Name, copiedAttachment.Name);
        Assert.Equal(originalAttachment.Target, copiedAttachment.Target);
        Assert.Equal(originalAttachment.IsLinked, copiedAttachment.IsLinked);

        var copiedChild = document.GetNode(Assert.Single(copied.ChildrenIds));
        Assert.NotEqual(child.Id, copiedChild.Id);
        Assert.Equal(["done"], copiedChild.Markers);

        Assert.True(history.Undo());
        Assert.DoesNotContain(copiedId, document.Nodes.Keys);
        Assert.True(history.Redo());
        var restored = document.GetNode(copiedId);
        Assert.Equal(copiedAttachment.Id, Assert.Single(restored.Attachments).Id);
        document.Validate();
    }
}
