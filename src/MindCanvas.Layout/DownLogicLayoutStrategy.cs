using MindCanvas.Core.Documents;
using MindCanvas.Layout.Geometry;

namespace MindCanvas.Layout;

public sealed class DownLogicLayoutStrategy : ILayoutStrategy
{
    private const double RootWidth = 156;
    private const double NodeWidth = 150;
    private const double NodeHeight = 48;
    private const double HorizontalGap = 24;
    private const double VerticalGap = 72;
    private const double Margin = 64;

    public string Id => "logic-down";

    public LayoutSnapshot Arrange(MindMapDocument document)
    {
        document.Validate();
        var subtreeWidths = new Dictionary<Guid, double>();

        double Measure(Guid id)
        {
            var node = document.GetNode(id);
            var ownWidth = id == document.RootNodeId ? RootWidth : NodeWidth;
            if (node.IsCollapsed || node.ChildrenIds.Count == 0)
                return subtreeWidths[id] = ownWidth;

            var childrenWidth = node.ChildrenIds.Sum(Measure) + HorizontalGap * (node.ChildrenIds.Count - 1);
            return subtreeWidths[id] = Math.Max(ownWidth, childrenWidth);
        }

        var totalWidth = Measure(document.RootNodeId);
        var nodes = new Dictionary<Guid, NodeLayout>();
        var connectors = new List<ConnectorLayout>();

        void Place(Guid id, int depth, double left, double y)
        {
            var node = document.GetNode(id);
            var width = id == document.RootNodeId ? RootWidth : NodeWidth;
            var blockWidth = subtreeWidths[id];
            var x = left + (blockWidth - width) / 2;
            var bounds = new RectD(x, y, width, NodeHeight);
            nodes[id] = new NodeLayout(id, bounds, depth);

            if (node.IsCollapsed || node.ChildrenIds.Count == 0)
                return;

            var childLeft = left;
            foreach (var childId in node.ChildrenIds)
            {
                var childWidth = subtreeWidths[childId];
                Place(childId, depth + 1, childLeft, bounds.Bottom + VerticalGap);
                var child = nodes[childId].Bounds;
                connectors.Add(new ConnectorLayout(id, childId, bounds.CenterX, bounds.Bottom, child.CenterX, child.Y));
                childLeft += childWidth + HorizontalGap;
            }
        }

        Place(document.RootNodeId, 0, Margin, Margin);
        var maxBottom = nodes.Values.Max(node => node.Bounds.Bottom) + Margin;
        return new LayoutSnapshot
        {
            Nodes = nodes,
            Connectors = connectors,
            CanvasBounds = new RectD(0, 0, totalWidth + Margin * 2, maxBottom)
        };
    }
}
