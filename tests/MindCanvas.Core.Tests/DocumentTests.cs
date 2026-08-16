using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Core.Tests;

public sealed class DocumentTests
{
    [Fact]
    public void Create_Add_Move_Validate()
    {
        var document = MindMapDocument.Create("Plan");
        var a = document.AddChild(document.RootNodeId, "A");
        var b = document.AddChild(document.RootNodeId, "B");
        var c = document.AddChild(a.Id, "C");
        document.MoveNode(c.Id, b.Id);
        document.Validate();
        Assert.Equal(b.Id, document.GetNode(c.Id).ParentId);
    }

    [Fact]
    public void UndoRedo_AddNode_RoundTrips()
    {
        var document = MindMapDocument.Create();
        var manager = new UndoRedoManager();
        var command = new AddNodeCommand(document, document.RootNodeId, "Child");
        manager.Execute(command);
        Assert.Equal(2, document.Nodes.Count);
        Assert.True(manager.Undo());
        Assert.Single(document.Nodes);
        Assert.True(manager.Redo());
        Assert.Equal(2, document.Nodes.Count);
        document.Validate();
    }

    [Fact]
    public void UndoRedo_AddNode_PreservesRequestedSiblingPosition()
    {
        var document = MindMapDocument.Create();
        var first = document.AddChild(document.RootNodeId, "First");
        var last = document.AddChild(document.RootNodeId, "Last");
        var manager = new UndoRedoManager();
        var command = new AddNodeCommand(document, document.RootNodeId, "Middle", 1);

        manager.Execute(command);
        var middleId = Assert.IsType<Guid>(command.CreatedNodeId);
        Assert.Equal(new[] { first.Id, middleId, last.Id }, document.Root.ChildrenIds);

        Assert.True(manager.Undo());
        Assert.True(manager.Redo());
        Assert.Equal(new[] { first.Id, middleId, last.Id }, document.Root.ChildrenIds);
        document.Validate();
    }

    [Fact]
    public void UndoRedo_DeleteNode_RestoresSubtreeAndSiblingOrder()
    {
        var document = MindMapDocument.Create();
        var first = document.AddChild(document.RootNodeId, "First");
        var middle = document.AddChild(document.RootNodeId, "Middle");
        var child = document.AddChild(middle.Id, "Nested");
        var last = document.AddChild(document.RootNodeId, "Last");
        var manager = new UndoRedoManager();

        manager.Execute(new DeleteNodeCommand(document, middle.Id));
        Assert.False(document.Nodes.ContainsKey(middle.Id));
        Assert.False(document.Nodes.ContainsKey(child.Id));
        Assert.Equal(new[] { first.Id, last.Id }, document.Root.ChildrenIds);

        Assert.True(manager.Undo());
        Assert.Equal(new[] { first.Id, middle.Id, last.Id }, document.Root.ChildrenIds);
        Assert.Equal(middle.Id, document.GetNode(child.Id).ParentId);
        document.Validate();

        Assert.True(manager.Redo());
        Assert.False(document.Nodes.ContainsKey(middle.Id));
        document.Validate();
    }

    [Fact]
    public void UndoRedo_Collapse_RoundTrips()
    {
        var document = MindMapDocument.Create();
        var branch = document.AddChild(document.RootNodeId, "Branch");
        document.AddChild(branch.Id, "Leaf");
        var manager = new UndoRedoManager();

        manager.Execute(new SetNodeCollapsedCommand(document, branch.Id, true));
        Assert.True(document.GetNode(branch.Id).IsCollapsed);
        Assert.Equal(2, document.EnumerateVisibleDepthFirst().Count());

        Assert.True(manager.Undo());
        Assert.False(document.GetNode(branch.Id).IsCollapsed);
        Assert.Equal(3, document.EnumerateVisibleDepthFirst().Count());
    }

    [Fact]
    public void UndoRedo_MoveNode_RestoresParentAndIndex()
    {
        var document = MindMapDocument.Create();
        var a = document.AddChild(document.RootNodeId, "A");
        var b = document.AddChild(document.RootNodeId, "B");
        var c = document.AddChild(document.RootNodeId, "C");
        var manager = new UndoRedoManager();

        manager.Execute(new MoveNodeCommand(document, b.Id, a.Id));
        Assert.Equal(a.Id, document.GetNode(b.Id).ParentId);
        Assert.Equal(new[] { a.Id, c.Id }, document.Root.ChildrenIds);

        Assert.True(manager.Undo());
        Assert.Equal(document.RootNodeId, document.GetNode(b.Id).ParentId);
        Assert.Equal(new[] { a.Id, b.Id, c.Id }, document.Root.ChildrenIds);
        document.Validate();
    }
}
