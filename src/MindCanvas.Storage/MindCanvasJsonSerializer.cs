using System.Text.Json;
using MindCanvas.Core.Documents;

namespace MindCanvas.Storage;

public sealed class MindCanvasJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string Serialize(MindMapDocument document)
    {
        document.Validate();
        return JsonSerializer.Serialize(new MindCanvasFileEnvelope { Document = document }, Options);
    }

    public MindMapDocument Deserialize(string json)
    {
        var envelope = JsonSerializer.Deserialize<MindCanvasFileEnvelope>(json, Options)
            ?? throw new InvalidDataException("The MindCanvas document is empty or invalid.");
        if (!string.Equals(envelope.Format, "MindCanvas", StringComparison.Ordinal))
            throw new InvalidDataException("The file is not a MindCanvas document.");
        if (envelope.SchemaVersion > MindMapDocument.CurrentSchemaVersion)
            throw new NotSupportedException($"Document schema {envelope.SchemaVersion} is newer than this app supports.");
        envelope.Document.Validate();
        return envelope.Document;
    }
}
