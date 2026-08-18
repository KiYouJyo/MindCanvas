using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Storage.Tests;

public sealed class ExchangeAndRecoveryTests
{
    [Fact]
    public void Markdown_round_trip_preserves_hierarchy()
    {
        var document = MindMapDocument.Create("Root");
        var first = document.AddChild(document.RootNodeId, "First");
        document.AddChild(first.Id, "Nested");
        document.AddChild(document.RootNodeId, "Second");
        var converter = new MarkdownMindMapConverter();

        var markdown = converter.Export(document);
        var imported = converter.Import(markdown);

        Assert.Equal("Root", imported.Root.Title);
        Assert.Equal(2, imported.Root.ChildrenIds.Count);
        var importedFirst = imported.GetNode(imported.Root.ChildrenIds[0]);
        Assert.Equal("First", importedFirst.Title);
        Assert.Single(importedFirst.ChildrenIds);
        Assert.Equal("Nested", imported.GetNode(importedFirst.ChildrenIds[0]).Title);
    }

    [Fact]
    public void Opml_round_trip_preserves_notes_tags_and_collapsed_state()
    {
        var document = MindMapDocument.Create("Root");
        var child = document.AddChild(document.RootNodeId, "Waterfront");
        document.SetNodeNotes(child.Id, "Evidence note");
        document.SetNodeTags(child.Id, ["Urban", "Waterfront"]);
        document.SetNodeCollapsed(child.Id, true);
        var converter = new OpmlMindMapConverter();

        var imported = converter.Import(converter.Export(document));
        var importedChild = imported.GetNode(imported.Root.ChildrenIds.Single());

        Assert.Equal("Evidence note", importedChild.Notes);
        Assert.Equal(2, importedChild.Tags.Count);
        Assert.True(importedChild.IsCollapsed);
    }

    [Fact]
    public async Task Autosave_service_can_enumerate_and_restore_snapshots()
    {
        var root = Path.Combine(Path.GetTempPath(), "MindCanvas.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var serializer = new MindCanvasJsonSerializer();
            var fileService = new MindCanvasFileService(serializer);
            var autosave = new AutosaveRecoveryService(fileService, root);
            var document = MindMapDocument.Create("Recover me");
            document.AddChild(document.RootNodeId, "Draft node");

            await autosave.SaveSnapshotAsync(document);
            var snapshots = await autosave.GetRecoverableSnapshotsAsync();
            var restored = await autosave.TryLoadSnapshotAsync(document.Id);

            Assert.Single(snapshots);
            Assert.NotNull(restored);
            Assert.Equal("Recover me", restored!.Title);
            Assert.True(autosave.DeleteSnapshot(document.Id));
            Assert.Null(await autosave.TryLoadSnapshotAsync(document.Id));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Drop_service_creates_link_image_and_structured_nodes()
    {
        var root = Path.Combine(Path.GetTempPath(), "MindCanvas.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var markdownPath = Path.Combine(root, "outline.md");
            var imagePath = Path.Combine(root, "photo.png");
            await File.WriteAllTextAsync(markdownPath, "# Imported\n## Child\n");
            await File.WriteAllBytesAsync(imagePath, [0x89, 0x50, 0x4e, 0x47]);

            var fileService = new MindCanvasFileService(new MindCanvasJsonSerializer());
            var exchange = new MindCanvasImportExportService(fileService, new MarkdownMindMapConverter(), new OpmlMindMapConverter());
            var drop = new DroppedContentService(exchange);
            var target = MindMapDocument.Create("Target");

            var created = await drop.AddAsync(target, target.RootNodeId, ["https://example.com/path", imagePath, markdownPath]);

            Assert.Equal(3, created.Count);
            Assert.Contains(target.Nodes.Values, node => node.Hyperlink == "https://example.com/path");
            Assert.Contains(target.Nodes.Values, node => node.Attachments.Any(item => item.Kind == NodeAttachmentKind.Image));
            var imported = target.Nodes.Values.Single(node => node.Title == "Imported");
            Assert.Single(imported.ChildrenIds);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
