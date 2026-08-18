using System.Text;
using Xunit;

namespace MindCanvas.Storage.Tests;

public sealed class RgbPdfExporterTests
{
    [Fact]
    public void Exports_valid_single_page_pdf_structure()
    {
        var pixels = new byte[]
        {
            255, 255, 255, 255, 0, 0,
            0, 255, 0, 0, 0, 255
        };

        var pdf = new RgbPdfExporter().Export(2, 2, pixels);
        var header = Encoding.ASCII.GetString(pdf.AsSpan(0, Math.Min(pdf.Length, 16)));
        var text = Encoding.ASCII.GetString(pdf);

        Assert.StartsWith("%PDF-1.4", header);
        Assert.Contains("/Subtype /Image", text);
        Assert.Contains("/Width 2 /Height 2", text);
        Assert.Contains("xref", text);
        Assert.EndsWith("%%EOF\n", text);
    }

    [Fact]
    public void Rejects_mismatched_pixel_buffer()
    {
        var exporter = new RgbPdfExporter();
        Assert.Throws<ArgumentException>(() => exporter.Export(2, 2, new byte[3]));
    }
}
