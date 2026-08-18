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

    [Fact]
    public void SubtreeTemplate_CapturesShapeAndMetadataWithoutDocumentIds()
    {
        var document = MindMapDocument.Create("Root");
        var branch = document.AddChild(document.RootNodeId, "Branch");
        branch.Notes = "note";
        branch.Hyperlink = "https://example.test";
        branch.IsCollapsed = true;
        var leaf = document.AddChild(branch.Id, "Leaf");
        leaf.Notes = "leaf note";

        var template = NodeSubtreeTemplate.Capture(document, branch.Id);

        Assert.Equal("Branch", template.Title);
        Assert.Equal("note", template.Notes);
        Assert.Equal("https://example.test", template.Hyperlink);
        Assert.True(template.IsCollapsed);
        var child = Assert.Single(template.Children);
        Assert.Equal("Leaf", child.Title);
        Assert.Equal("leaf note", child.Notes);
    }

    [Fact]
    public void InsertSubtree_UsesFreshIdsAndPreservesRequestedSiblingPosition()
    {
        var source = MindMapDocument.Create("Source");
        var sourceBranch = source.AddChild(source.RootNodeId, "Copied");
        source.AddChild(sourceBranch.Id, "Nested");
        var template = NodeSubtreeTemplate.Capture(source, sourceBranch.Id);

        var target = MindMapDocument.Create("Target");
        var first = target.AddChild(target.RootNodeId, "First");
        var last = target.AddChild(target.RootNodeId, "Last");
        var command = new InsertSubtreeCommand(target, target.RootNodeId, template, 1);
        command.Execute();

        var copiedId = Assert.IsType<Guid>(command.CreatedRootId);
        Assert.NotEqual(sourceBranch.Id, copiedId);
        Assert.Equal(new[] { first.Id, copiedId, last.Id }, target.Root.ChildrenIds);
        var copied = target.GetNode(copiedId);
        Assert.Equal("Copied", copied.Title);
        var nestedId = Assert.Single(copied.ChildrenIds);
        Assert.NotEqual(sourceBranch.ChildrenIds[0], nestedId);
        Assert.Equal("Nested", target.GetNode(nestedId).Title);
        target.Validate();
    }

    [Fact]
    public void UndoRedo_InsertSubtree_RestoresIdsOrderAndMetadata()
    {
        var document = MindMapDocument.Create("Target");
        var first = document.AddChild(document.RootNodeId, "First");
        var last = document.AddChild(document.RootNodeId, "Last");
        var template = new NodeSubtreeTemplate(
            "Copied",
            "note",
            "https://example.test",
            true,
            new[] { new NodeSubtreeTemplate("Child", null, null, false, Array.Empty<NodeSubtreeTemplate>()) });
        var manager = new UndoRedoManager();
        var command = new InsertSubtreeCommand(document, document.RootNodeId, template, 1);

        manager.Execute(command);
        var copiedId = Assert.IsType<Guid>(command.CreatedRootId);
        var childId = Assert.Single(document.GetNode(copiedId).ChildrenIds);
        Assert.True(manager.Undo());
        Assert.False(document.Nodes.ContainsKey(copiedId));

        Assert.True(manager.Redo());
        Assert.Equal(new[] { first.Id, copiedId, last.Id }, document.Root.ChildrenIds);
        var copied = document.GetNode(copiedId);
        Assert.Equal("note", copied.Notes);
        Assert.Equal("https://example.test", copied.Hyperlink);
        Assert.True(copied.IsCollapsed);
        Assert.Equal(childId, Assert.Single(copied.ChildrenIds));
        document.Validate();
    }
}
