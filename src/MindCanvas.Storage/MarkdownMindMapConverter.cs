using System.Text;
using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed class MarkdownMindMapConverter
{
    public string Export(MindMapDocument document)
    {
        document.Validate();
        var builder = new StringBuilder();
        Write(document.RootNodeId, 1);
        return builder.ToString().TrimEnd() + Environment.NewLine;

        void Write(Guid nodeId, int depth)
        {
            var node = document.GetNode(nodeId);
            builder.Append('#', Math.Clamp(depth, 1, 6)).Append(' ').AppendLine(EscapeTitle(node.Title));
            if (!string.IsNullOrWhiteSpace(node.Notes))
            {
                builder.AppendLine();
                foreach (var line in node.Notes!.Replace("\r\n", "\n").Split('\n'))
                    builder.Append("> ").AppendLine(line);
                builder.AppendLine();
            }
            foreach (var childId in node.ChildrenIds)
                Write(childId, depth + 1);
        }
    }

    public MindMapDocument Import(string markdown, string fallbackTitle = "Imported Markdown")
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var headings = ParseHeadings(markdown).ToArray();
        if (headings.Length == 0)
            return MindMapDocument.Create(fallbackTitle);

        var first = headings[0];
        var document = MindMapDocument.Create(first.Title);
        var stack = new List<(int Level, Guid NodeId)> { (first.Level, document.RootNodeId) };

        foreach (var heading in headings.Skip(1))
        {
            while (stack.Count > 0 && stack[^1].Level >= heading.Level)
                stack.RemoveAt(stack.Count - 1);
            var parentId = stack.Count == 0 ? document.RootNodeId : stack[^1].NodeId;
            var node = document.AddChild(parentId, heading.Title);
            stack.Add((heading.Level, node.Id));
        }

        document.SchemaVersion = MindMapDocument.CurrentSchemaVersion;
        return document;
    }

    private static IEnumerable<(int Level, string Title)> ParseHeadings(string markdown)
    {
        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimStart();
            var level = 0;
            while (level < line.Length && level < 6 && line[level] == '#')
                level++;
            if (level == 0 || level >= line.Length || !char.IsWhiteSpace(line[level]))
                continue;
            var title = line[(level + 1)..].Trim();
            if (title.Length > 0)
                yield return (level, title);
        }
    }

    private static string EscapeTitle(string title) =>
        string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Replace("\r", " ").Replace("\n", " ").Trim();
}
