using MindCanvas.Core.Documents;
using Xunit;

namespace MindCanvas.Storage.Tests;

public sealed class SerializationTests
{
    [Fact]
    public void Json_RoundTrip_Preserves_Tree()
    {
        var document = MindMapDocument.Create("Research");
        document.AddChild(document.RootNodeId, "Methods");
        var serializer = new MindCanvasJsonSerializer();
        var reloaded = serializer.Deserialize(serializer.Serialize(document));
        reloaded.Validate();
        Assert.Equal(document.Title, reloaded.Title);
        Assert.Equal(2, reloaded.Nodes.Count);
    }
}
