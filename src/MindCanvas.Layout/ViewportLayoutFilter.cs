using MindCanvas.Layout.Geometry;

namespace MindCanvas.Layout;

public sealed record ViewportLayoutSlice(
    IReadOnlyDictionary<Guid, NodeLayout> Nodes,
    IReadOnlyList<ConnectorLayout> Connectors,
    RectD Viewport,
    bool IsVirtualized);

public sealed class ViewportLayoutFilter(
    int virtualizationThreshold = 180,
    double overscan = 220)
{
    public int VirtualizationThreshold { get; } = Math.Max(1, virtualizationThreshold);
    public double Overscan { get; } = Math.Max(0, overscan);

    public ViewportLayoutSlice Filter(
        LayoutSnapshot snapshot,
        RectD viewport,
        Guid? pinnedNodeId = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Nodes.Count <= VirtualizationThreshold || viewport.Width <= 0 || viewport.Height <= 0)
            return new ViewportLayoutSlice(snapshot.Nodes, snapshot.Connectors, viewport, false);

        var expanded = Inflate(viewport, Overscan);
        var visibleIds = snapshot.Nodes.Values
            .Where(node => Intersects(node.Bounds, expanded))
            .Select(node => node.NodeId)
            .ToHashSet();

        if (pinnedNodeId is Guid pinned && snapshot.Nodes.ContainsKey(pinned))
            visibleIds.Add(pinned);

        var connectors = snapshot.Connectors
            .Where(connector => visibleIds.Contains(connector.ParentId) || visibleIds.Contains(connector.ChildId))
            .ToArray();

        foreach (var connector in connectors)
        {
            if (snapshot.Nodes.ContainsKey(connector.ParentId))
                visibleIds.Add(connector.ParentId);
            if (snapshot.Nodes.ContainsKey(connector.ChildId))
                visibleIds.Add(connector.ChildId);
        }

        var nodes = snapshot.Nodes
            .Where(pair => visibleIds.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        return new ViewportLayoutSlice(nodes, connectors, viewport, true);
    }

    private static RectD Inflate(RectD rect, double amount) =>
        new(rect.X - amount, rect.Y - amount, rect.Width + amount * 2, rect.Height + amount * 2);

    private static bool Intersects(RectD a, RectD b) =>
        a.Right >= b.X && a.X <= b.Right && a.Bottom >= b.Y && a.Y <= b.Bottom;
}
