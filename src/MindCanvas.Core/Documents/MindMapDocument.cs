namespace MindCanvas.Core.Documents;

public sealed class MindMapDocument
{
    public const int CurrentSchemaVersion = 1;

    public Guid Id { get; set; } = Guid.NewGuid();
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string Title { get; set; } = "Untitled";
    public Guid RootNodeId { get; set; }
    public Dictionary<Guid, MindNode> Nodes { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    public static MindMapDocument Create(string title = "Untitled")
    {
        var root = new MindNode { Title = title };
        return new MindMapDocument
        {
            Title = title,
            RootNodeId = root.Id,
            Nodes = new Dictionary<Guid, MindNode> { [root.Id] = root }
        };
    }

    public MindNode Root => GetNode(RootNodeId);

    public MindNode GetNode(Guid id) =>
        Nodes.TryGetValue(id, out var node)
            ? node
            : throw new KeyNotFoundException($"Node '{id}' does not exist.");

    public MindNode AddChild(Guid parentId, string title, int? index = null)
    {
        var parent = GetNode(parentId);
        var child = new MindNode { ParentId = parentId, Title = title };
        Nodes.Add(child.Id, child);

        if (index is >= 0 && index <= parent.ChildrenIds.Count)
            parent.ChildrenIds.Insert(index.Value, child.Id);
        else
            parent.ChildrenIds.Add(child.Id);

        Touch();
        return child;
    }

    public void RenameNode(Guid nodeId, string title)
    {
        GetNode(nodeId).Title = title;
        if (nodeId == RootNodeId)
            Title = string.IsNullOrWhiteSpace(title) ? Title : title;
        Touch();
    }

    public void SetNodeCollapsed(Guid nodeId, bool isCollapsed)
    {
        var node = GetNode(nodeId);
        if (node.IsCollapsed == isCollapsed)
            return;

        node.IsCollapsed = isCollapsed;
        Touch();
    }

    public void MoveNode(Guid nodeId, Guid newParentId, int? index = null)
    {
        if (nodeId == RootNodeId)
            throw new InvalidOperationException("The root node cannot be moved.");
        if (nodeId == newParentId || IsDescendant(newParentId, nodeId))
            throw new InvalidOperationException("A node cannot be moved into itself or one of its descendants.");

        var node = GetNode(nodeId);
        var newParent = GetNode(newParentId);
        if (node.ParentId is Guid oldParentId)
            GetNode(oldParentId).ChildrenIds.Remove(nodeId);

        node.ParentId = newParentId;
        if (index is >= 0 && index <= newParent.ChildrenIds.Count)
            newParent.ChildrenIds.Insert(index.Value, nodeId);
        else
            newParent.ChildrenIds.Add(nodeId);
        Touch();
    }

    public IReadOnlyList<MindNode> RemoveSubtree(Guid nodeId)
    {
        if (nodeId == RootNodeId)
            throw new InvalidOperationException("The root node cannot be removed.");

        var node = GetNode(nodeId);
        if (node.ParentId is Guid parentId)
            GetNode(parentId).ChildrenIds.Remove(nodeId);

        var removed = new List<MindNode>();
        Collect(nodeId, removed);
        foreach (var item in removed)
            Nodes.Remove(item.Id);
        Touch();
        return removed;
    }

    public void RestoreSubtree(IEnumerable<MindNode> nodes)
    {
        var snapshot = nodes.Select(n => n.Clone()).ToArray();
        foreach (var node in snapshot)
            Nodes[node.Id] = node;

        var root = snapshot.Single(n => n.ParentId is null || !snapshot.Any(x => x.Id == n.ParentId));
        if (root.ParentId is Guid parentId)
        {
            var parent = GetNode(parentId);
            if (!parent.ChildrenIds.Contains(root.Id))
                parent.ChildrenIds.Add(root.Id);
        }
        Touch();
    }

    public IEnumerable<MindNode> EnumerateDepthFirst()
    {
        var stack = new Stack<Guid>();
        stack.Push(RootNodeId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            var node = GetNode(id);
            yield return node;
            for (var i = node.ChildrenIds.Count - 1; i >= 0; i--)
                stack.Push(node.ChildrenIds[i]);
        }
    }

    public IEnumerable<MindNode> EnumerateVisibleDepthFirst()
    {
        var stack = new Stack<Guid>();
        stack.Push(RootNodeId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            var node = GetNode(id);
            yield return node;
            if (node.IsCollapsed)
                continue;
            for (var i = node.ChildrenIds.Count - 1; i >= 0; i--)
                stack.Push(node.ChildrenIds[i]);
        }
    }

    public void Validate()
    {
        if (!Nodes.ContainsKey(RootNodeId))
            throw new InvalidDataException("The document root does not exist.");
        if (Root.ParentId is not null)
            throw new InvalidDataException("The root node must not have a parent.");

        var visited = new HashSet<Guid>();
        void Visit(Guid id)
        {
            if (!visited.Add(id))
                throw new InvalidDataException("The document contains a cycle or duplicate child reference.");
            var node = GetNode(id);
            foreach (var childId in node.ChildrenIds)
            {
                var child = GetNode(childId);
                if (child.ParentId != id)
                    throw new InvalidDataException("Parent/child references are inconsistent.");
                Visit(childId);
            }
        }
        Visit(RootNodeId);
        if (visited.Count != Nodes.Count)
            throw new InvalidDataException("The document contains unreachable nodes.");
    }

    private bool IsDescendant(Guid candidateId, Guid ancestorId)
    {
        var current = GetNode(candidateId);
        while (current.ParentId is Guid parentId)
        {
            if (parentId == ancestorId)
                return true;
            current = GetNode(parentId);
        }
        return false;
    }

    private void Collect(Guid id, ICollection<MindNode> output)
    {
        var node = GetNode(id);
        output.Add(node.Clone());
        foreach (var child in node.ChildrenIds)
            Collect(child, output);
    }

    private void Touch() => ModifiedAt = DateTimeOffset.UtcNow;
}
