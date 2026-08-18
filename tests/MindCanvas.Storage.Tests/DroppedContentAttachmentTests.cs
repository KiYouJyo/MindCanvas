using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Storage.Tests;

public sealed class DroppedContentAttachmentTests
{
    [Fact]
    public async Task Node_targeted_drop_attaches_files_and_urls_but_merges_structured_content()
    {
        var root = Path.Combine(Path.GetTempPath(), "MindCanvas.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var imagePath = Path.Combine(root, "site.png");
            var markdownPath = Path.Combine(root, "outline.md");
            await File.WriteAllBytesAsync(imagePath, [0x89, 0x50, 0x4e, 0x47], TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(markdownPath, "# Imported\n## Child\n", TestContext.Current.CancellationToken);

            var fileService = new MindCanvasFileService(new MindCanvasJsonSerializer());
            var exchange = new MindCanvasImportExportService(fileService, new MarkdownMindMapConverter(), new OpmlMindMapConverter());
            var service = new DroppedContentService(exchange);
            var document = MindMapDocument.Create("Root");
            var target = document.AddChild(document.RootNodeId, "Target");

            var result = await service.AttachAsync(
                document,
                target.Id,
                ["https://example.com/reference", imagePath, markdownPath],
                TestContext.Current.CancellationToken);

            Assert.Equal(2, result.AttachmentIds.Count);
            Assert.Single(result.CreatedNodeIds);
            Assert.Equal(2, document.GetNode(target.Id).Attachments.Count);
            Assert.Contains(document.GetNode(target.Id).Attachments, attachment => attachment.Kind == NodeAttachmentKind.Link);
            Assert.Contains(document.GetNode(target.Id).Attachments, attachment => attachment.Kind == NodeAttachmentKind.Image);

            var imported = document.GetNode(result.CreatedNodeIds.Single());
            Assert.Equal("Imported", imported.Title);
            Assert.Equal(target.Id, imported.ParentId);
            Assert.Single(imported.ChildrenIds);
            document.Validate();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
