using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Layout.Tests;

public sealed class AdditionalLayoutTests
{
    [Fact]
    public void Down_layout_places_descendants_below_their_parent()
    {
        var document = MindMapDocument.Create("Root");
        var child = document.AddChild(document.RootNodeId, "Child");
        var grandChild = document.AddChild(child.Id, "Grandchild");

        var snapshot = new DownLogicLayoutStrategy().Arrange(document);
        var rootBounds = snapshot.Nodes[document.RootNodeId].Bounds;
        var childBounds = snapshot.Nodes[child.Id].Bounds;
        var grandChildBounds = snapshot.Nodes[grandChild.Id].Bounds;

        Assert.True(childBounds.Y > rootBounds.Bottom);
        Assert.True(grandChildBounds.Y > childBounds.Bottom);
        Assert.Equal(2, snapshot.Connectors.Count);
    }

    [Fact]
    public void Balanced_layout_uses_both_sides_when_root_has_multiple_branches()
    {
        var document = MindMapDocument.Create("Root");
        var first = document.AddChild(document.RootNodeId, "First");
        var second = document.AddChild(document.RootNodeId, "Second");

        var snapshot = new BalancedMindMapLayoutStrategy().Arrange(document);
        var root = snapshot.Nodes[document.RootNodeId].Bounds;
        var branchBounds = new[] { snapshot.Nodes[first.Id].Bounds, snapshot.Nodes[second.Id].Bounds };

        Assert.Contains(branchBounds, bounds => bounds.Right < root.X);
        Assert.Contains(branchBounds, bounds => bounds.X > root.Right);
        Assert.Equal(2, snapshot.Connectors.Count);
        Assert.All(snapshot.Nodes.Values, node =>
        {
            Assert.True(node.Bounds.X >= 0);
            Assert.True(node.Bounds.Y >= 0);
        });
    }

    [Fact]
    public void Additional_layouts_respect_collapsed_nodes()
    {
        var document = MindMapDocument.Create("Root");
        var child = document.AddChild(document.RootNodeId, "Child");
        var hidden = document.AddChild(child.Id, "Hidden");
        document.SetNodeCollapsed(child.Id, true);

        foreach (var strategy in new ILayoutStrategy[] { new DownLogicLayoutStrategy(), new BalancedMindMapLayoutStrategy() })
        {
            var snapshot = strategy.Arrange(document);
            Assert.Contains(child.Id, snapshot.Nodes.Keys);
            Assert.DoesNotContain(hidden.Id, snapshot.Nodes.Keys);
        }
    }
}
