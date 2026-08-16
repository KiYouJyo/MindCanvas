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
}
