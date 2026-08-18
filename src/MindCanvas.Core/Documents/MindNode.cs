namespace MindCanvas.Core.Documents;

public enum NodePriority
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public enum NodeAttachmentKind
{
    File,
    Image,
    Link
}

public sealed record NodeAttachment(
    Guid Id,
    NodeAttachmentKind Kind,
    string Name,
    string Target,
    bool IsLinked = true)
{
    public static NodeAttachment Create(NodeAttachmentKind kind, string name, string target, bool isLinked = true) =>
        new(Guid.NewGuid(), kind, name, target, isLinked);
}

public sealed class MindNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public List<Guid> ChildrenIds { get; set; } = [];
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Hyperlink { get; set; }
    public NodePriority Priority { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> Markers { get; set; } = [];
    public List<NodeAttachment> Attachments { get; set; } = [];
    public bool IsCollapsed { get; set; }

    public MindNode Clone() => new()
    {
        Id = Id,
        ParentId = ParentId,
        ChildrenIds = [.. ChildrenIds],
        Title = Title,
        Notes = Notes,
        Hyperlink = Hyperlink,
        Priority = Priority,
        Tags = [.. Tags],
        Markers = [.. Markers],
        Attachments = [.. Attachments],
        IsCollapsed = IsCollapsed
    };
}
