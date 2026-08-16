namespace MindCanvas.Core.Documents;

public sealed class MindNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public List<Guid> ChildrenIds { get; set; } = [];
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Hyperlink { get; set; }
    public bool IsCollapsed { get; set; }

    public MindNode Clone() => new()
    {
        Id = Id,
        ParentId = ParentId,
        ChildrenIds = [.. ChildrenIds],
        Title = Title,
        Notes = Notes,
        Hyperlink = Hyperlink,
        IsCollapsed = IsCollapsed
    };
}
