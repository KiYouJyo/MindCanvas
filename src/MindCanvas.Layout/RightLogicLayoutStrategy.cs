using MindCanvas.Core.Documents;
using MindCanvas.Layout.Geometry;

namespace MindCanvas.Layout;

public sealed class RightLogicLayoutStrategy : ILayoutStrategy
{
    private static readonly LayoutSnapshotCache Cache = new();
    private const double RootWidth = 156;
    private const double NodeWidth = 150;
    private const double NodeHeight = 48;
    private const double HorizontalGap = 80;
    private const double VerticalGap = 20;
    private const double Margin = 64;

    public string Id => "logic-right";

    public LayoutSnapshot Arrange(MindMapDocument document)
    {
        var strategyId = LayoutRuntime.CurrentId;
        var focusId = LayoutRuntime.FocusRootNodeId;
        return Cache.GetOrCreate(document, strategyId, focusId, () => ArrangeUncached(document, strategyId, focusId));
    }

    private static LayoutSnapshot ArrangeUncached(MindMapDocument document, string strategyId, Guid? focusId)
    {
        var source = focusId is Guid id && document.Nodes.ContainsKey(id)
            ? DocumentProjection.CreateFocused(document, id)
            : document;

        return strategyId switch
        {
            "mindmap-balanced" => new BalancedMindMapLayoutStrategy().Arrange(source),
            "logic-down" => new DownLogicLayoutStrategy().Arrange(source),
            _ => ArrangeRight(source)
        };
    }

    private static LayoutSnapshot ArrangeRight(MindMapDocument document)
    {
        document.Validate();
        var subtreeHeights = new Dictionary<Guid, double>();
        double Measure(Guid id)
        {
            var node = document.GetNode(id);
            if (node.IsCollapsed || node.ChildrenIds.Count == 0)
                return subtreeHeights[id] = NodeHeight;
            var children = node.ChildrenIds.Select(Measure).ToArray();
            return subtreeHeights[id] = Math.Max(NodeHeight, children.Sum() + VerticalGap * (children.Length - 1));
        }

        var totalHeight = Measure(document.RootNodeId);
        var nodes = new Dictionary<Guid, NodeLayout>();
        var connectors = new List<ConnectorLayout>();

        void Place(Guid id, int depth, double x, double top)
        {
            var node = document.GetNode(id);
            var width = depth == 0 ? RootWidth : NodeWidth;
            var blockHeight = subtreeHeights[id];
            var y = top + (blockHeight - NodeHeight) / 2;
            var bounds = new RectD(x, y, width, NodeHeight);
            nodes[id] = new NodeLayout(id, bounds, depth);

            if (node.IsCollapsed || node.ChildrenIds.Count == 0) return;
            var childTop = top;
            foreach (var childId in node.ChildrenIds)
            {
                var childHeight = subtreeHeights[childId];
                var childX = bounds.Right + HorizontalGap;
                Place(childId, depth + 1, childX, childTop);
                var child = nodes[childId].Bounds;
                connectors.Add(new ConnectorLayout(id, childId, bounds.Right, bounds.CenterY, child.X, child.CenterY));
                childTop += childHeight + VerticalGap;
            }
        }

        Place(document.RootNodeId, 0, Margin, Margin);
        var maxRight = nodes.Values.Max(n => n.Bounds.Right) + Margin;
        return new LayoutSnapshot
        {
            Nodes = nodes,
            Connectors = connectors,
            CanvasBounds = new RectD(0, 0, maxRight, totalHeight + Margin * 2)
        };
    }
}
