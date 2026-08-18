using System.Globalization;
using System.Net;
using System.Text;
using MindCanvas.Core.Documents;

namespace MindCanvas.Layout;

public sealed class SvgMindMapExporter
{
    public string Export(MindMapDocument document, ILayoutStrategy? strategy = null)
    {
        strategy ??= new RightLogicLayoutStrategy();
        var snapshot = strategy.Arrange(document);
        var width = Math.Max(1, snapshot.CanvasBounds.Width);
        var height = Math.Max(1, snapshot.CanvasBounds.Height);
        var builder = new StringBuilder();
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
            .Append(F(width)).Append(' ').Append(F(height)).Append("\" width=\"")
            .Append(F(width)).Append("\" height=\"").Append(F(height)).AppendLine("\">");
        builder.AppendLine("  <rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");

        foreach (var connector in snapshot.Connectors)
        {
            builder.Append("  <line x1=\"").Append(F(connector.StartX))
                .Append("\" y1=\"").Append(F(connector.StartY))
                .Append("\" x2=\"").Append(F(connector.EndX))
                .Append("\" y2=\"").Append(F(connector.EndY))
                .AppendLine("\" stroke=\"#6b9fd1\" stroke-width=\"1.5\"/>");
        }

        foreach (var layout in snapshot.Nodes.Values.OrderBy(node => node.Depth))
        {
            var node = document.GetNode(layout.NodeId);
            var bounds = layout.Bounds;
            builder.Append("  <g data-node-id=\"").Append(node.Id).AppendLine("\">");
            builder.Append("    <rect x=\"").Append(F(bounds.X)).Append("\" y=\"").Append(F(bounds.Y))
                .Append("\" width=\"").Append(F(bounds.Width)).Append("\" height=\"").Append(F(bounds.Height))
                .AppendLine("\" rx=\"6\" fill=\"#ffffff\" stroke=\"#d9dfe5\"/>");
            builder.Append("    <rect x=\"").Append(F(bounds.X)).Append("\" y=\"").Append(F(bounds.Y))
                .Append("\" width=\"6\" height=\"").Append(F(bounds.Height))
                .AppendLine("\" rx=\"3\" fill=\"#086bc2\"/>");
            builder.Append("    <text x=\"").Append(F(bounds.X + 18)).Append("\" y=\"").Append(F(bounds.CenterY + 5))
                .Append("\" font-family=\"Segoe UI, sans-serif\" font-size=\"14\" fill=\"#1a1c21\">")
                .Append(WebUtility.HtmlEncode(node.Title)).AppendLine("</text>");
            builder.AppendLine("  </g>");
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
