using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Core.Tests;

public sealed class DeleteNodesCommandTests
{
    [Fact]
    public void Deletes_multiple_siblings_and_restores_original_order()
    {
        var document = MindMapDocument.Create("Root");
        var first = document.AddChild(document.RootNodeId, "First");
        var second = document.AddChild(document.RootNodeId, "Second");
        var third = document.AddChild(document.RootNodeId, "Third");
        var history = new UndoRedoManager();

        history.Execute(new DeleteNodesCommand(document, [first.Id, third.Id]));

        Assert.Equal([second.Id], document.Root.ChildrenIds);
        Assert.False(document.Nodes.ContainsKey(first.Id));
        Assert.False(document.Nodes.ContainsKey(third.Id));

        Assert.True(history.Undo());
        Assert.Equal([first.Id, second.Id, third.Id], document.Root.ChildrenIds);
        Assert.True(history.Redo());
        Assert.Equal([second.Id], document.Root.ChildrenIds);
    }

    [Fact]
    public void Selecting_parent_and_descendant_deletes_subtree_once()
    {
        var document = MindMapDocument.Create("Root");
        var branch = document.AddChild(document.RootNodeId, "Branch");
        var leaf = document.AddChild(branch.Id, "Leaf");
        var other = document.AddChild(document.RootNodeId, "Other");
        var command = new DeleteNodesCommand(document, [branch.Id, leaf.Id]);

        command.Execute();

        Assert.Equal([other.Id], document.Root.ChildrenIds);
        Assert.False(document.Nodes.ContainsKey(branch.Id));
        Assert.False(document.Nodes.ContainsKey(leaf.Id));

        command.Undo();
        Assert.Equal([branch.Id, other.Id], document.Root.ChildrenIds);
        Assert.Equal([leaf.Id], document.GetNode(branch.Id).ChildrenIds);
        document.Validate();
    }
}
