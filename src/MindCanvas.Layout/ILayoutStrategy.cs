using MindCanvas.Core.Documents;
using MindCanvas.Layout.Geometry;

namespace MindCanvas.Layout;

public interface ILayoutStrategy
{
    string Id { get; }
    LayoutSnapshot Arrange(MindMapDocument document);
}
