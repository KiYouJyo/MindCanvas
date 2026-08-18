using MindCanvas.Core.Documents;
using MindCanvas.Core.Search;
using Xunit;

namespace MindCanvas.Core.Tests;

public sealed class SearchAndMetadataTests
{
    [Fact]
    public void Metadata_operations_normalize_labels_and_clone_attachments()
    {
        var document = MindMapDocument.Create("Root");
        var node = document.AddChild(document.RootNodeId, "Waterfront");

        document.SetNodeNotes(node.Id, "  Detailed research note  ");
        document.SetNodePriority(node.Id, NodePriority.High);
        document.SetNodeTags(node.Id, ["Urban", " waterfront ", "URBAN", ""]);
        document.SetNodeMarkers(node.Id, ["Question", "Important", "question"]);
        var attachment = document.AddNodeAttachment(node.Id, NodeAttachmentKind.File, "brief.pdf", "C:/brief.pdf");

        var updated = document.GetNode(node.Id);
        Assert.Equal("Detailed research note", updated.Notes);
        Assert.Equal(NodePriority.High, updated.Priority);
        Assert.Equal(2, updated.Tags.Count);
        Assert.Contains("Urban", updated.Tags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("waterfront", updated.Tags, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(2, updated.Markers.Count);
        Assert.Single(updated.Attachments);
        Assert.Equal(attachment.Id, updated.Clone().Attachments.Single().Id);
        Assert.Equal(2, document.SchemaVersion);
    }

    [Fact]
    public void Search_finds_title_notes_and_tags_across_documents()
    {
        var first = MindMapDocument.Create("Study A");
        var waterfront = first.AddChild(first.RootNodeId, "Waterfront strategy");
        first.SetNodeNotes(waterfront.Id, "Compare public-space performance.");
        first.SetNodeTags(waterfront.Id, ["Urban renewal", "Waterfront"]);

        var second = MindMapDocument.Create("Study B");
        var japanese = second.AddChild(second.RootNodeId, "Japanese cases");
        second.SetNodeNotes(japanese.Id, "Waterfront regeneration references.");

        var service = new NodeSearchService();
        var hits = service.Search(
            [new DocumentSearchSource(first, "a.mcanvas"), new DocumentSearchSource(second, "b.mcanvas")],
            "waterfront");

        Assert.Contains(hits, hit => hit.NodeId == waterfront.Id && hit.Field == NodeSearchField.Title);
        Assert.Contains(hits, hit => hit.NodeId == waterfront.Id && hit.Field == NodeSearchField.Tag);
        Assert.Contains(hits, hit => hit.NodeId == japanese.Id && hit.Field == NodeSearchField.Notes);
        Assert.Contains(hits, hit => hit.SourcePath == "b.mcanvas");
    }

    [Fact]
    public void Search_can_require_tags_and_tag_index_counts_documents()
    {
        var first = MindMapDocument.Create("First");
        var one = first.AddChild(first.RootNodeId, "Public space");
        first.SetNodeTags(one.Id, ["Urban", "Research"]);
        var two = first.AddChild(first.RootNodeId, "Mobility");
        first.SetNodeTags(two.Id, ["Urban"]);

        var second = MindMapDocument.Create("Second");
        var three = second.AddChild(second.RootNodeId, "Urban policy");
        second.SetNodeTags(three.Id, ["Urban"]);

        var sources = new[] { new DocumentSearchSource(first), new DocumentSearchSource(second) };
        var hits = new NodeSearchService().Search(sources, "public", new NodeSearchOptions(RequiredTags: ["Research"]));
        var tags = new TagIndexService().Build(sources);

        Assert.Single(hits);
        var urban = Assert.Single(tags, tag => tag.Name.Equals("Urban", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, urban.NodeCount);
        Assert.Equal(2, urban.DocumentCount);
    }
}
