using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed class MindCanvasFileEnvelope
{
    public string Format { get; set; } = "MindCanvas";
    public int SchemaVersion { get; set; } = MindMapDocument.CurrentSchemaVersion;
    public string AppVersion { get; set; } = "0.1.0";
    public required MindMapDocument Document { get; set; }
}
