using System.IO.Compression;
using System.Text;
using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Storage.Tests;

public sealed class ExtendedExchangeTests
{
    private static MindMapDocument CreateDocument()
    {
        var document = MindMapDocument.Create("Project");
        var research = document.AddChild(document.RootNodeId, "Research");
        document.SetNodeNotes(research.Id, "Evidence and references");
        document.SetNodeHyperlink(research.Id, "https://example.com");
        document.SetNodeTags(research.Id, ["urban", "research"]);
        document.AddChild(research.Id, "Interviews");
        document.AddChild(document.RootNodeId, "Design");
        return document;
    }

    [Fact]
    public void FreeMind_round_trip_preserves_tree_and_metadata()
    {
        var converter = new FreeMindMindMapConverter();
        var imported = converter.Import(converter.Export(CreateDocument()));
        var research = imported.GetNode(imported.Root.ChildrenIds[0]);

        Assert.Equal("Project", imported.Root.Title);
        Assert.Equal("Research", research.Title);
        Assert.Equal("Evidence and references", research.Notes);
        Assert.Equal("https://example.com", research.Hyperlink);
        Assert.Contains("urban", research.Tags, StringComparer.OrdinalIgnoreCase);
        Assert.Single(research.ChildrenIds);
    }

    [Fact]
    public void Mermaid_round_trip_preserves_hierarchy_and_escaped_titles()
    {
        var document = CreateDocument();
        document.RenameNode(document.Root.ChildrenIds[1], "Design (Phase 1)");
        var converter = new MermaidMindMapConverter();
        var text = converter.Export(document);
        var imported = converter.Import(text);

        Assert.StartsWith("mindmap", text);
        Assert.Equal(2, imported.Root.ChildrenIds.Count);
        Assert.Equal("Design (Phase 1)", imported.GetNode(imported.Root.ChildrenIds[1]).Title);
        Assert.Single(imported.GetNode(imported.Root.ChildrenIds[0]).ChildrenIds);
    }

    [Fact]
    public async Task XMind_round_trip_preserves_tree_notes_and_link()
    {
        var root = Path.Combine(Path.GetTempPath(), "MindCanvas.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "roundtrip.xmind");
        try
        {
            var converter = new XMindMindMapConverter();
            await converter.ExportAsync(CreateDocument(), path, TestContext.Current.CancellationToken);
            var imported = await converter.ImportAsync(path, TestContext.Current.CancellationToken);
            var research = imported.GetNode(imported.Root.ChildrenIds[0]);

            Assert.True(File.Exists(path));
            Assert.Equal("Project", imported.Root.Title);
            Assert.Equal("Evidence and references", research.Notes);
            Assert.Equal("https://example.com", research.Hyperlink);
            Assert.Single(research.ChildrenIds);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task XMind_imports_legacy_content_xml_packages()
    {
        var root = Path.Combine(Path.GetTempPath(), "MindCanvas.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "legacy.xmind");
        try
        {
            await using (var stream = File.Create(path))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("content.xml");
                await using var entryStream = entry.Open();
                var xml = """
                    <?xml version="1.0" encoding="UTF-8"?>
                    <xmap-content xmlns="urn:xmind:xmap:xmlns:content:2.0" xmlns:xlink="http://www.w3.org/1999/xlink">
                      <sheet id="sheet1">
                        <title>Legacy Sheet</title>
                        <topic id="root">
                          <title>Legacy Root</title>
                          <children><topics type="attached">
                            <topic id="research" xlink:href="https://example.com/legacy">
                              <title>Research</title>
                              <notes><plain>Legacy note</plain></notes>
                              <children><topics type="attached"><topic id="child"><title>Interview</title></topic></topics></children>
                            </topic>
                          </topics></children>
                        </topic>
                      </sheet>
                    </xmap-content>
                    """;
                var bytes = Encoding.UTF8.GetBytes(xml);
                await entryStream.WriteAsync(bytes, TestContext.Current.CancellationToken);
            }

            var imported = await new XMindMindMapConverter().ImportAsync(path, TestContext.Current.CancellationToken);
            var research = imported.GetNode(imported.Root.ChildrenIds.Single());

            Assert.Equal("Legacy Root", imported.Root.Title);
            Assert.Equal("Research", research.Title);
            Assert.Equal("Legacy note", research.Notes);
            Assert.Equal("https://example.com/legacy", research.Hyperlink);
            Assert.Equal("Interview", imported.GetNode(research.ChildrenIds.Single()).Title);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("test.xmind", MindCanvasExchangeFormat.XMind)]
    [InlineData("test.mm", MindCanvasExchangeFormat.FreeMind)]
    [InlineData("test.mmd", MindCanvasExchangeFormat.Mermaid)]
    [InlineData("test.mermaid", MindCanvasExchangeFormat.Mermaid)]
    public void Detects_extended_exchange_formats(string path, MindCanvasExchangeFormat expected)
        => Assert.Equal(expected, MindCanvasImportExportService.DetectFormat(path));
}
