using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Core.Tests;

public sealed class MoveNodeCommandTests
{
    [Fact]
    public void Reorders_siblings_and_undoes_to_original_position()
    {
        var document = MindMapDocument.Create("Root");
        var first = document.AddChild(document.RootNodeId, "First");
        var second = document.AddChild(document.RootNodeId, "Second");
        var third = document.AddChild(document.RootNodeId, "Third");
        var history = new UndoRedoManager();

        history.Execute(new MoveNodeCommand(document, first.Id, document.RootNodeId, 1));

        Assert.Equal([second.Id, first.Id, third.Id], document.Root.ChildrenIds);
        Assert.True(history.Undo());
        Assert.Equal([first.Id, second.Id, third.Id], document.Root.ChildrenIds);
        Assert.True(history.Redo());
        Assert.Equal([second.Id, first.Id, third.Id], document.Root.ChildrenIds);
        document.Validate();
    }

    [Fact]
    public void Reparents_node_and_restores_old_parent_on_undo()
    {
        var document = MindMapDocument.Create("Root");
        var left = document.AddChild(document.RootNodeId, "Left");
        var right = document.AddChild(document.RootNodeId, "Right");
        var child = document.AddChild(left.Id, "Child");
        var history = new UndoRedoManager();

        history.Execute(new MoveNodeCommand(document, child.Id, right.Id));

        Assert.Empty(document.GetNode(left.Id).ChildrenIds);
        Assert.Equal([child.Id], document.GetNode(right.Id).ChildrenIds);
        Assert.Equal(right.Id, document.GetNode(child.Id).ParentId);

        Assert.True(history.Undo());
        Assert.Equal([child.Id], document.GetNode(left.Id).ChildrenIds);
        Assert.Empty(document.GetNode(right.Id).ChildrenIds);
        Assert.Equal(left.Id, document.GetNode(child.Id).ParentId);
        document.Validate();
    }

    [Fact]
    public void Moving_parent_into_descendant_is_rejected_without_mutating_tree()
    {
        var document = MindMapDocument.Create("Root");
        var parent = document.AddChild(document.RootNodeId, "Parent");
        var child = document.AddChild(parent.Id, "Child");
        var beforeRevision = document.Revision;

        Assert.Throws<InvalidOperationException>(() => new MoveNodeCommand(document, parent.Id, child.Id).Execute());

        Assert.Equal(document.RootNodeId, document.GetNode(parent.Id).ParentId);
        Assert.Equal(parent.Id, document.GetNode(child.Id).ParentId);
        Assert.Equal(beforeRevision, document.Revision);
        document.Validate();
    }

    [Fact]
    public void Root_cannot_be_moved()
    {
        var document = MindMapDocument.Create("Root");
        var child = document.AddChild(document.RootNodeId, "Child");

        Assert.Throws<InvalidOperationException>(() => new MoveNodeCommand(document, document.RootNodeId, child.Id).Execute());
        document.Validate();
    }
}
