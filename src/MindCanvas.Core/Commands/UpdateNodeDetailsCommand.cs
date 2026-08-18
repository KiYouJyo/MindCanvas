using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

public sealed class UpdateNodeDetailsCommand(
    MindMapDocument document,
    Guid nodeId,
    string? notes,
    string? hyperlink,
    NodePriority priority,
    IReadOnlyCollection<string> tags,
    IReadOnlyCollection<string> markers) : IUndoableCommand
{
    private NodeDetailsSnapshot? _before;

    public string Description => "Update node details";

    public void Execute()
    {
        _before ??= NodeDetailsSnapshot.Capture(document.GetNode(nodeId));
        Apply(notes, hyperlink, priority, tags, markers);
    }

    public void Undo()
    {
        if (_before is null)
            return;
        Apply(_before.Notes, _before.Hyperlink, _before.Priority, _before.Tags, _before.Markers);
    }

    private void Apply(
        string? nextNotes,
        string? nextHyperlink,
        NodePriority nextPriority,
        IEnumerable<string> nextTags,
        IEnumerable<string> nextMarkers)
    {
        document.SetNodeNotes(nodeId, nextNotes);
        document.SetNodeHyperlink(nodeId, nextHyperlink);
        document.SetNodePriority(nodeId, nextPriority);
        document.SetNodeTags(nodeId, nextTags);
        document.SetNodeMarkers(nodeId, nextMarkers);
    }

    private sealed record NodeDetailsSnapshot(
        string? Notes,
        string? Hyperlink,
        NodePriority Priority,
        IReadOnlyList<string> Tags,
        IReadOnlyList<string> Markers)
    {
        public static NodeDetailsSnapshot Capture(MindNode node) =>
            new(node.Notes, node.Hyperlink, node.Priority, [.. node.Tags], [.. node.Markers]);
    }
}
