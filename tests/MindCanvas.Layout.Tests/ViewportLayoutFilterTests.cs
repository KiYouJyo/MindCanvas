using MindCanvas.Layout.Geometry;
using Xunit;

namespace MindCanvas.Layout.Tests;

public sealed class ViewportLayoutFilterTests
{
    [Fact]
    public void Small_maps_are_not_virtualized()
    {
        var snapshot = Snapshot(10);
        var slice = new ViewportLayoutFilter(virtualizationThreshold: 20).Filter(
            snapshot,
            new RectD(0, 0, 200, 200));

        Assert.False(slice.IsVirtualized);
        Assert.Equal(snapshot.Nodes.Count, slice.Nodes.Count);
        Assert.Equal(snapshot.Connectors.Count, slice.Connectors.Count);
    }

    [Fact]
    public void Large_maps_keep_viewport_nodes_and_overscan_neighbors()
    {
        var snapshot = Snapshot(30);
        var slice = new ViewportLayoutFilter(virtualizationThreshold: 10, overscan: 50).Filter(
            snapshot,
            new RectD(300, 0, 200, 80));

        Assert.True(slice.IsVirtualized);
        Assert.True(slice.Nodes.Count < snapshot.Nodes.Count);
        Assert.Contains(snapshot.Nodes.Keys.ElementAt(3), slice.Nodes.Keys);
        Assert.Contains(snapshot.Nodes.Keys.ElementAt(5), slice.Nodes.Keys);
    }

    [Fact]
    public void Pinned_node_is_preserved_outside_viewport()
    {
        var snapshot = Snapshot(30);
        var pinned = snapshot.Nodes.Keys.Last();
        var slice = new ViewportLayoutFilter(virtualizationThreshold: 10, overscan: 0).Filter(
            snapshot,
            new RectD(0, 0, 120, 80),
            pinned);

        Assert.True(slice.IsVirtualized);
        Assert.Contains(pinned, slice.Nodes.Keys);
    }

    private static LayoutSnapshot Snapshot(int count)
    {
        var nodes = new Dictionary<Guid, NodeLayout>();
        var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
        for (var index = 0; index < count; index++)
            nodes[ids[index]] = new NodeLayout(ids[index], new RectD(index * 100, 10, 80, 40), index);

        var connectors = new List<ConnectorLayout>();
        for (var index = 1; index < count; index++)
        {
            var parent = nodes[ids[index - 1]].Bounds;
            var child = nodes[ids[index]].Bounds;
            connectors.Add(new ConnectorLayout(ids[index - 1], ids[index], parent.Right, parent.CenterY, child.X, child.CenterY));
        }

        return new LayoutSnapshot
        {
            Nodes = nodes,
            Connectors = connectors,
            CanvasBounds = new RectD(0, 0, count * 100, 100)
        };
    }
}
