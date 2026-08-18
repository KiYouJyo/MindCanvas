using System.Xml.Linq;
using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed class OpmlMindMapConverter
{
    public string Export(MindMapDocument document)
    {
        document.Validate();
        var root = new XElement("opml",
            new XAttribute("version", "2.0"),
            new XElement("head", new XElement("title", document.Title)),
            new XElement("body", BuildOutline(document.RootNodeId)));
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();

        XElement BuildOutline(Guid nodeId)
        {
            var node = document.GetNode(nodeId);
            var element = new XElement("outline",
                new XAttribute("text", node.Title),
                new XAttribute("_mindcanvasId", node.Id));
            if (!string.IsNullOrWhiteSpace(node.Notes))
                element.SetAttributeValue("_note", node.Notes);
            if (node.IsCollapsed)
                element.SetAttributeValue("_isCollapsed", "true");
            foreach (var tag in node.Tags)
                element.Add(new XElement("_tag", tag));
            foreach (var childId in node.ChildrenIds)
                element.Add(BuildOutline(childId));
            return element;
        }
    }

    public MindMapDocument Import(string opml, string fallbackTitle = "Imported OPML")
    {
        ArgumentNullException.ThrowIfNull(opml);
        var xml = XDocument.Parse(opml, LoadOptions.PreserveWhitespace);
        var body = xml.Root?.Element("body") ?? throw new InvalidDataException("The OPML document has no body element.");
        var rootOutline = body.Elements("outline").FirstOrDefault();
        if (rootOutline is null)
            return MindMapDocument.Create(xml.Root?.Element("head")?.Element("title")?.Value ?? fallbackTitle);

        var rootTitle = GetTitle(rootOutline, fallbackTitle);
        var document = MindMapDocument.Create(rootTitle);
        ApplyMetadata(document.Root, rootOutline);
        ImportChildren(rootOutline, document.RootNodeId);
        document.SchemaVersion = MindMapDocument.CurrentSchemaVersion;
        return document;

        void ImportChildren(XElement source, Guid parentId)
        {
            foreach (var childElement in source.Elements("outline"))
            {
                var child = document.AddChild(parentId, GetTitle(childElement, "Untitled"));
                ApplyMetadata(child, childElement);
                ImportChildren(childElement, child.Id);
            }
        }
    }

    private static string GetTitle(XElement element, string fallback) =>
        element.Attribute("text")?.Value?.Trim() is { Length: > 0 } title ? title : fallback;

    private static void ApplyMetadata(MindNode node, XElement element)
    {
        node.Notes = element.Attribute("_note")?.Value;
        node.IsCollapsed = string.Equals(element.Attribute("_isCollapsed")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        node.Tags = element.Elements("_tag")
            .Select(tag => tag.Value.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
