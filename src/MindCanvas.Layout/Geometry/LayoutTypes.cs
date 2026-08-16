namespace MindCanvas.Layout.Geometry;

public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + Width / 2;
    public double CenterY => Y + Height / 2;
}

public sealed record NodeLayout(Guid NodeId, RectD Bounds, int Depth);
public sealed record ConnectorLayout(Guid ParentId, Guid ChildId, double StartX, double StartY, double EndX, double EndY);

public sealed class LayoutSnapshot
{
    public required IReadOnlyDictionary<Guid, NodeLayout> Nodes { get; init; }
    public required IReadOnlyList<ConnectorLayout> Connectors { get; init; }
    public required RectD CanvasBounds { get; init; }
}
