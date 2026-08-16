using MindCanvas.Core.Documents;
using Xunit;

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

    [Fact]
    public void Collapsed_Branch_Hides_Descendants_From_Layout()
    {
        var document = MindMapDocument.Create("Root");
        var branch = document.AddChild(document.RootNodeId, "Branch");
        var leaf = document.AddChild(branch.Id, "Leaf");
        document.SetNodeCollapsed(branch.Id, true);

        var snapshot = new RightLogicLayoutStrategy().Arrange(document);

        Assert.Contains(document.RootNodeId, snapshot.Nodes.Keys);
        Assert.Contains(branch.Id, snapshot.Nodes.Keys);
        Assert.DoesNotContain(leaf.Id, snapshot.Nodes.Keys);
        Assert.Single(snapshot.Connectors);
    }
}
