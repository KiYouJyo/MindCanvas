using MindCanvas.Core.Documents;

namespace MindCanvas.Layout.Tests;

public sealed class RightLogicLayoutTests
{
    [Fact]
    public void Children_Appear_To_Right_Of_Parent()
    {
        var document = MindMapDocument.Create("Root");
        var child = document.AddChild(document.RootNodeId, "Child");
        var snapshot = new RightLogicLayoutStrategy().Arrange(document);
        Assert.True(snapshot.Nodes[child.Id].Bounds.X > snapshot.Nodes[document.RootNodeId].Bounds.Right);
        Assert.Single(snapshot.Connectors);
    }
}
