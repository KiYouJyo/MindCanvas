using MindCanvas.Core.Documents;
using MindCanvas.Layout.Geometry;
using Xunit;

namespace MindCanvas.Layout.Tests;

public sealed class LayoutCacheTests
{
    [Fact]
    public void Cache_reuses_snapshot_until_document_revision_changes()
    {
        var document = MindMapDocument.Create("Root");
        var cache = new LayoutSnapshotCache();
        var calls = 0;

        LayoutSnapshot Factory()
        {
            calls++;
            return new RightLogicLayoutStrategy().Arrange(document);
        }

        var first = cache.GetOrCreate(document, "logic-right", null, Factory);
        var second = cache.GetOrCreate(document, "logic-right", null, Factory);

        Assert.Same(first, second);
        Assert.Equal(1, calls);

        var before = document.Revision;
        document.AddChild(document.RootNodeId, "Child");
        Assert.True(document.Revision > before);

        var third = cache.GetOrCreate(document, "logic-right", null, Factory);
        Assert.NotSame(first, third);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Cache_separates_layout_and_focus_modes()
    {
        var document = MindMapDocument.Create("Root");
        var child = document.AddChild(document.RootNodeId, "Child");
        var cache = new LayoutSnapshotCache();
        var calls = 0;
        LayoutSnapshot Factory()
        {
            calls++;
            return new LayoutSnapshot
            {
                Nodes = new Dictionary<Guid, NodeLayout>(),
                Connectors = [],
                CanvasBounds = new RectD(0, 0, calls, calls)
            };
        }

        var right = cache.GetOrCreate(document, "logic-right", null, Factory);
        var balanced = cache.GetOrCreate(document, "mindmap-balanced", null, Factory);
        var focused = cache.GetOrCreate(document, "logic-right", child.Id, Factory);

        Assert.Equal(3, calls);
        Assert.NotSame(right, balanced);
        Assert.NotSame(right, focused);
    }
}
