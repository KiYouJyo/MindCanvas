using MindCanvas.Core.Documents;

namespace MindCanvas.Core.Commands;

public sealed class DeleteNodesCommand(
    MindMapDocument document,
    IReadOnlyCollection<Guid> nodeIds) : IUndoableCommand
{
    private List<Removal>? _removals;

    public string Description => "Delete nodes";

    public void Execute()
    {
        if (_removals is null)
            _removals = CaptureRemovals();

        foreach (var removal in _removals
                     .OrderBy(item => item.ParentId)
                     .ThenByDescending(item => item.Index))
        {
            if (document.Nodes.ContainsKey(removal.RootId))
                document.RemoveSubtree(removal.RootId);
        }
    }

    public void Undo()
    {
        if (_removals is null)
            return;

        foreach (var removal in _removals
                     .OrderBy(item => item.ParentId)
                     .ThenBy(item => item.Index))
        {
            document.RestoreSubtree(removal.Nodes);
            var parent = document.GetNode(removal.ParentId);
            parent.ChildrenIds.Remove(removal.RootId);
            parent.ChildrenIds.Insert(Math.Clamp(removal.Index, 0, parent.ChildrenIds.Count), removal.RootId);
        }
    }

    private List<Removal> CaptureRemovals()
    {
        var selected = nodeIds
            .Where(id => id != document.RootNodeId && document.Nodes.ContainsKey(id))
            .ToHashSet();
        if (selected.Count == 0)
            return [];

        var roots = selected.Where(id => !HasSelectedAncestor(id, selected)).ToArray();
        var removals = new List<Removal>(roots.Length);
        foreach (var rootId in roots)
        {
            var root = document.GetNode(rootId);
            if (root.ParentId is not Guid parentId)
                continue;
            var parent = document.GetNode(parentId);
            var index = parent.ChildrenIds.IndexOf(rootId);
            var snapshots = CaptureSubtree(rootId).ToArray();
            removals.Add(new Removal(rootId, parentId, Math.Max(0, index), snapshots));
        }
        return removals;
    }

    private bool HasSelectedAncestor(Guid nodeId, HashSet<Guid> selected)
    {
        var current = document.GetNode(nodeId);
        while (current.ParentId is Guid parentId)
        {
            if (selected.Contains(parentId))
                return true;
            current = document.GetNode(parentId);
        }
        return false;
    }

    private IEnumerable<MindNode> CaptureSubtree(Guid rootId)
    {
        var stack = new Stack<Guid>();
        stack.Push(rootId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            var node = document.GetNode(id);
            yield return node.Clone();
            for (var index = node.ChildrenIds.Count - 1; index >= 0; index--)
                stack.Push(node.ChildrenIds[index]);
        }
    }

    private sealed record Removal(
        Guid RootId,
        Guid ParentId,
        int Index,
        IReadOnlyList<MindNode> Nodes);
}
