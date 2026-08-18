using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Core.Tests;

public sealed class FocusAndDetailsCommandTests
{
    [Fact]
    public void Focus_projection_keeps_only_selected_subtree_and_builds_breadcrumb()
    {
        var document = MindMapDocument.Create("Root");
        var branch = document.AddChild(document.RootNodeId, "Branch");
        var leaf = document.AddChild(branch.Id, "Leaf");
        document.AddChild(document.RootNodeId, "Other");

        var breadcrumb = DocumentProjection.GetBreadcrumb(document, leaf.Id);
        var focused = DocumentProjection.CreateFocused(document, branch.Id);

        Assert.Equal(["Root", "Branch", "Leaf"], breadcrumb.Select(node => node.Title).ToArray());
        Assert.Equal(branch.Id, focused.RootNodeId);
        Assert.Null(focused.Root.ParentId);
        Assert.Equal(2, focused.Nodes.Count);
        Assert.Contains(leaf.Id, focused.Nodes.Keys);
    }

    [Fact]
    public void Update_details_command_is_undoable()
    {
        var document = MindMapDocument.Create("Root");
        var node = document.AddChild(document.RootNodeId, "Node");
        document.SetNodeNotes(node.Id, "Before");
        document.SetNodeTags(node.Id, ["old"]);
        var history = new UndoRedoManager();

        history.Execute(new UpdateNodeDetailsCommand(
            document,
            node.Id,
            "After",
            "https://example.com",
            NodePriority.High,
            ["new", "research"],
            ["important"]));

        Assert.Equal("After", document.GetNode(node.Id).Notes);
        Assert.Equal(NodePriority.High, document.GetNode(node.Id).Priority);
        Assert.True(history.Undo());
        Assert.Equal("Before", document.GetNode(node.Id).Notes);
        Assert.Equal(NodePriority.None, document.GetNode(node.Id).Priority);
        Assert.Equal(["old"], document.GetNode(node.Id).Tags);
        Assert.True(history.Redo());
        Assert.Equal("After", document.GetNode(node.Id).Notes);
    }
}
