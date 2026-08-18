using MindCanvas.Core.Documents;
using MindCanvas.Layout.Geometry;

namespace MindCanvas.Layout;

public sealed class LayoutSnapshotCache(int capacity = 64)
{
    private readonly int _capacity = Math.Max(4, capacity);
    private readonly Dictionary<CacheKey, LayoutSnapshot> _entries = [];
    private readonly Queue<CacheKey> _order = [];
    private readonly object _gate = new();

    public LayoutSnapshot GetOrCreate(
        MindMapDocument document,
        string strategyId,
        Guid? focusRootNodeId,
        Func<LayoutSnapshot> factory)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        ArgumentNullException.ThrowIfNull(factory);
        var key = new CacheKey(document.Id, document.Revision, strategyId, focusRootNodeId);

        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var existing))
                return existing;
        }

        var created = factory();
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var raced))
                return raced;

            _entries[key] = created;
            _order.Enqueue(key);
            while (_entries.Count > _capacity && _order.Count > 0)
            {
                var oldest = _order.Dequeue();
                _entries.Remove(oldest);
            }
        }
        return created;
    }

    public void Invalidate(Guid documentId)
    {
        lock (_gate)
        {
            foreach (var key in _entries.Keys.Where(key => key.DocumentId == documentId).ToArray())
                _entries.Remove(key);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
            _order.Clear();
        }
    }

    private readonly record struct CacheKey(Guid DocumentId, long Revision, string StrategyId, Guid? FocusRootNodeId);
}
