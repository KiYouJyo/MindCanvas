using System.Text;
using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed class MermaidMindMapConverter
{
    public string Export(MindMapDocument document)
    {
        document.Validate();
        var builder = new StringBuilder();
        builder.AppendLine("mindmap");
        Write(document.RootNodeId, 1);
        return builder.ToString();

        void Write(Guid nodeId, int depth)
        {
            var node = document.GetNode(nodeId);
            builder.Append(' ', depth * 2).AppendLine(Escape(node.Title));
            foreach (var childId in node.ChildrenIds)
                Write(childId, depth + 1);
        }
    }

    public MindMapDocument Import(string mermaid, string fallbackTitle = "Imported Mermaid")
    {
        ArgumentNullException.ThrowIfNull(mermaid);
        var rows = mermaid.Replace("\r\n", "\n").Split('\n')
            .Select(ParseRow)
            .Where(row => row is not null)
            .Select(row => row!.Value)
            .ToArray();
        if (rows.Length == 0)
            return MindMapDocument.Create(fallbackTitle);

        var first = rows[0];
        var document = MindMapDocument.Create(first.Title);
        var stack = new List<(int Indent, Guid NodeId)> { (first.Indent, document.RootNodeId) };
        foreach (var row in rows.Skip(1))
        {
            while (stack.Count > 0 && stack[^1].Indent >= row.Indent)
                stack.RemoveAt(stack.Count - 1);
            var parent = stack.Count == 0 ? document.RootNodeId : stack[^1].NodeId;
            var node = document.AddChild(parent, row.Title);
            stack.Add((row.Indent, node.Id));
        }
        document.SchemaVersion = MindMapDocument.CurrentSchemaVersion;
        return document;
    }

    private static (int Indent, string Title)? ParseRow(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var trimmed = raw.Trim();
        if (trimmed.Equals("mindmap", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("%%"))
            return null;
        var indent = raw.TakeWhile(char.IsWhiteSpace).Count();
        var title = Unwrap(trimmed);
        return title.Length == 0 ? null : (indent, title);
    }

    private static string Escape(string value)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? "Untitled" : value.Replace("\r", " ").Replace("\n", " ").Trim();
        return clean.IndexOfAny(['(', ')', '[', ']', '{', '}', '"']) >= 0
            ? $"[\"{clean.Replace("\"", "\\\"")}\"]"
            : clean;
    }

    private static string Unwrap(string value)
    {
        if (value.StartsWith("[\"") && value.EndsWith("\"]") && value.Length >= 4)
            return value[2..^2].Replace("\\\"", "\"").Trim();
        if ((value.StartsWith("((") && value.EndsWith("))")) ||
            (value.StartsWith("{{") && value.EndsWith("}}")))
            return value[2..^2].Trim();
        if ((value.StartsWith('(') && value.EndsWith(')')) ||
            (value.StartsWith('[') && value.EndsWith(']')) ||
            (value.StartsWith('{') && value.EndsWith('}')))
            return value[1..^1].Trim().Trim('"');
        return value.Trim('"');
    }
}
