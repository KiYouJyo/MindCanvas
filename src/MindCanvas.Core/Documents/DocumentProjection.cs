namespace MindCanvas.Core.Documents;

public static class DocumentProjection
{
    public static IReadOnlyList<MindNode> GetBreadcrumb(MindMapDocument document, Guid nodeId)
    {
        var path = new List<MindNode>();
        var current = document.GetNode(nodeId);
        while (true)
        {
            path.Add(current);
            if (current.ParentId is not Guid parentId)
                break;
            current = document.GetNode(parentId);
        }
        path.Reverse();
        return path;
    }

    public static MindMapDocument CreateFocused(MindMapDocument source, Guid rootNodeId)
    {
        source.Validate();
        var sourceRoot = source.GetNode(rootNodeId);
        var focused = new MindMapDocument
        {
            Id = source.Id,
            SchemaVersion = source.SchemaVersion,
            Revision = source.Revision,
            Title = sourceRoot.Title,
            RootNodeId = rootNodeId,
            CreatedAt = source.CreatedAt,
            ModifiedAt = source.ModifiedAt,
            Nodes = []
        };

        Copy(rootNodeId, isRoot: true);
        focused.Validate();
        return focused;

        void Copy(Guid id, bool isRoot)
        {
            var clone = source.GetNode(id).Clone();
            if (isRoot)
                clone.ParentId = null;
            focused.Nodes[id] = clone;
            foreach (var childId in clone.ChildrenIds)
                Copy(childId, isRoot: false);
        }
    }
}
