using MindCanvas.Core.Documents;
using MindCanvas.Layout.Geometry;

namespace MindCanvas.Layout;

public sealed class BalancedMindMapLayoutStrategy : ILayoutStrategy
{
    private const double RootWidth = 156;
    private const double NodeWidth = 150;
    private const double NodeHeight = 48;
    private const double HorizontalGap = 80;
    private const double VerticalGap = 20;
    private const double Margin = 64;

    public string Id => "mindmap-balanced";

    public LayoutSnapshot Arrange(MindMapDocument document)
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

        Measure(document.RootNodeId);
        var root = document.Root;
        var left = new List<Guid>();
        var right = new List<Guid>();
        double leftHeight = 0;
        double rightHeight = 0;
        foreach (var childId in root.ChildrenIds.OrderByDescending(id => subtreeHeights[id]))
        {
            if (leftHeight <= rightHeight)
            {
                left.Add(childId);
                leftHeight += subtreeHeights[childId] + VerticalGap;
            }
            else
            {
                right.Add(childId);
                rightHeight += subtreeHeights[childId] + VerticalGap;
            }
        }

        var nodes = new Dictionary<Guid, NodeLayout>();
        var connectors = new List<ConnectorLayout>();
        var totalHeight = Math.Max(NodeHeight, Math.Max(GroupHeight(left), GroupHeight(right)));
        var rootBounds = new RectD(0, (totalHeight - NodeHeight) / 2, RootWidth, NodeHeight);
        nodes[root.Id] = new NodeLayout(root.Id, rootBounds, 0);

        PlaceGroup(left, -1);
        PlaceGroup(right, 1);

        var minX = nodes.Values.Min(item => item.Bounds.X);
        var minY = nodes.Values.Min(item => item.Bounds.Y);
        var maxRight = nodes.Values.Max(item => item.Bounds.Right);
        var maxBottom = nodes.Values.Max(item => item.Bounds.Bottom);
        var offsetX = Margin - minX;
        var offsetY = Margin - minY;

        var normalizedNodes = nodes.ToDictionary(
            pair => pair.Key,
            pair => pair.Value with
            {
                Bounds = new RectD(
                    pair.Value.Bounds.X + offsetX,
                    pair.Value.Bounds.Y + offsetY,
                    pair.Value.Bounds.Width,
                    pair.Value.Bounds.Height)
            });
        var normalizedConnectors = connectors.Select(connector => connector with
        {
            StartX = connector.StartX + offsetX,
            StartY = connector.StartY + offsetY,
            EndX = connector.EndX + offsetX,
            EndY = connector.EndY + offsetY
        }).ToArray();

        return new LayoutSnapshot
        {
            Nodes = normalizedNodes,
            Connectors = normalizedConnectors,
            CanvasBounds = new RectD(0, 0, maxRight - minX + Margin * 2, maxBottom - minY + Margin * 2)
        };

        double GroupHeight(IReadOnlyCollection<Guid> ids) =>
            ids.Count == 0 ? 0 : ids.Sum(id => subtreeHeights[id]) + VerticalGap * (ids.Count - 1);

        void PlaceGroup(IReadOnlyList<Guid> ids, int direction)
        {
            var top = (totalHeight - GroupHeight(ids)) / 2;
            foreach (var childId in ids)
            {
                var height = subtreeHeights[childId];
                PlaceSide(childId, 1, direction, top, rootBounds);
                top += height + VerticalGap;
            }
        }

        void PlaceSide(Guid id, int depth, int direction, double top, RectD parentBounds)
        {
            var node = document.GetNode(id);
            var blockHeight = subtreeHeights[id];
            var y = top + (blockHeight - NodeHeight) / 2;
            var x = direction > 0
                ? parentBounds.Right + HorizontalGap
                : parentBounds.X - HorizontalGap - NodeWidth;
            var bounds = new RectD(x, y, NodeWidth, NodeHeight);
            nodes[id] = new NodeLayout(id, bounds, depth);
            connectors.Add(direction > 0
                ? new ConnectorLayout(parentBounds == rootBounds ? root.Id : node.ParentId!.Value, id, parentBounds.Right, parentBounds.CenterY, bounds.X, bounds.CenterY)
                : new ConnectorLayout(parentBounds == rootBounds ? root.Id : node.ParentId!.Value, id, parentBounds.X, parentBounds.CenterY, bounds.Right, bounds.CenterY));

            if (node.IsCollapsed || node.ChildrenIds.Count == 0)
                return;

            var childTop = top;
            foreach (var childId in node.ChildrenIds)
            {
                var childHeight = subtreeHeights[childId];
                PlaceSide(childId, depth + 1, direction, childTop, bounds);
                childTop += childHeight + VerticalGap;
            }
        }
    }
}
