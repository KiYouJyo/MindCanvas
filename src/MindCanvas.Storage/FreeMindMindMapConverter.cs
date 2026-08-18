using System.Xml.Linq;
using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed class FreeMindMindMapConverter
{
    public string Export(MindMapDocument document)
    {
        document.Validate();
        var map = new XElement("map",
            new XAttribute("version", "1.0.1"),
            BuildNode(document.RootNodeId));
        return new XDocument(new XDeclaration("1.0", "UTF-8", null), map).ToString();

        XElement BuildNode(Guid nodeId)
        {
            var node = document.GetNode(nodeId);
            var element = new XElement("node",
                new XAttribute("TEXT", string.IsNullOrWhiteSpace(node.Title) ? "Untitled" : node.Title),
                new XAttribute("ID", node.Id.ToString("N")));
            if (node.IsCollapsed)
                element.SetAttributeValue("FOLDED", "true");
            if (!string.IsNullOrWhiteSpace(node.Hyperlink))
                element.SetAttributeValue("LINK", node.Hyperlink);
            if (!string.IsNullOrWhiteSpace(node.Notes))
                element.Add(new XElement("richcontent",
                    new XAttribute("TYPE", "NOTE"),
                    new XElement("html", new XElement("body", new XElement("p", node.Notes)))));
            foreach (var tag in node.Tags)
                element.Add(new XElement("attribute", new XAttribute("NAME", "tag"), new XAttribute("VALUE", tag)));
            foreach (var childId in node.ChildrenIds)
                element.Add(BuildNode(childId));
            return element;
        }
    }

    public MindMapDocument Import(string xml, string fallbackTitle = "Imported FreeMind")
    {
        ArgumentNullException.ThrowIfNull(xml);
        var source = XDocument.Parse(xml);
        var rootElement = source.Root?.Element("node") ?? throw new InvalidDataException("The FreeMind document has no root node.");
        var document = MindMapDocument.Create(GetTitle(rootElement, fallbackTitle));
        ApplyMetadata(document.Root, rootElement);
        ImportChildren(rootElement, document.RootNodeId);
        document.SchemaVersion = MindMapDocument.CurrentSchemaVersion;
        return document;

        void ImportChildren(XElement parentElement, Guid parentId)
        {
            foreach (var element in parentElement.Elements("node"))
            {
                var node = document.AddChild(parentId, GetTitle(element, "Untitled"));
                ApplyMetadata(node, element);
                ImportChildren(element, node.Id);
            }
        }
    }

    private static string GetTitle(XElement node, string fallback) =>
        node.Attribute("TEXT")?.Value?.Trim() is { Length: > 0 } value ? value : fallback;

    private static void ApplyMetadata(MindNode node, XElement element)
    {
        node.IsCollapsed = string.Equals(element.Attribute("FOLDED")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        node.Hyperlink = element.Attribute("LINK")?.Value;
        node.Tags = element.Elements("attribute")
            .Where(attribute => string.Equals(attribute.Attribute("NAME")?.Value, "tag", StringComparison.OrdinalIgnoreCase))
            .Select(attribute => attribute.Attribute("VALUE")?.Value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var note = element.Elements("richcontent")
            .FirstOrDefault(item => string.Equals(item.Attribute("TYPE")?.Value, "NOTE", StringComparison.OrdinalIgnoreCase));
        node.Notes = note is null ? null : string.Join(" ", note.DescendantNodes().OfType<XText>().Select(text => text.Value.Trim()).Where(text => text.Length > 0));
    }
}
