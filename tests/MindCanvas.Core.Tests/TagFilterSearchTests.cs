using MindCanvas.Core.Documents;
using MindCanvas.Core.Search;
using Xunit;

namespace MindCanvas.Core.Tests;

public sealed class TagFilterSearchTests
{
    [Fact]
    public void Empty_text_query_can_filter_nodes_by_required_tag()
    {
        var document = MindMapDocument.Create("Root");
        var first = document.AddChild(document.RootNodeId, "First");
        var second = document.AddChild(document.RootNodeId, "Second");
        document.SetNodeTags(first.Id, ["Urban", "Research"]);
        document.SetNodeTags(second.Id, ["Urban"]);

        var hits = new NodeSearchService().Search(
            document,
            string.Empty,
            new NodeSearchOptions(RequiredTags: ["Research"]));

        var hit = Assert.Single(hits);
        Assert.Equal(first.Id, hit.NodeId);
        Assert.Equal(NodeSearchField.Tag, hit.Field);
        Assert.Equal("Research", hit.MatchText);
    }
}
