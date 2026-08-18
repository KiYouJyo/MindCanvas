namespace MindCanvas.Layout;

public sealed class LayoutStrategyCatalog
{
    private readonly IReadOnlyDictionary<string, ILayoutStrategy> _strategies;

    public LayoutStrategyCatalog(IEnumerable<ILayoutStrategy>? strategies = null)
    {
        var items = (strategies ??
        [
            new RightLogicLayoutStrategy(),
            new BalancedMindMapLayoutStrategy(),
            new DownLogicLayoutStrategy()
        ]).ToArray();
        _strategies = items.ToDictionary(strategy => strategy.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> Ids => _strategies.Keys.ToArray();

    public ILayoutStrategy Resolve(string? id) =>
        id is not null && _strategies.TryGetValue(id, out var strategy)
            ? strategy
            : _strategies["logic-right"];
}
