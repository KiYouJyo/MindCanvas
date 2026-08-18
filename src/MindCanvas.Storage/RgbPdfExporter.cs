using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace MindCanvas.Storage;

public sealed class RgbPdfExporter
{
    public byte[] Export(int width, int height, ReadOnlySpan<byte> rgbPixels, double dpi = 96)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
        if (rgbPixels.Length != checked(width * height * 3))
            throw new ArgumentException("RGB buffer length does not match image dimensions.", nameof(rgbPixels));

        byte[] compressed;
        using (var compressedStream = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                zlib.Write(rgbPixels);
            compressed = compressedStream.ToArray();
        }

        var pageWidth = width * 72d / dpi;
        var pageHeight = height * 72d / dpi;
        var content = Encoding.ASCII.GetBytes(
            $"q\n{F(pageWidth)} 0 0 {F(pageHeight)} 0 0 cm\n/Im0 Do\nQ\n");

        using var output = new MemoryStream();
        var offsets = new long[6];
        Write(output, "%PDF-1.4\n%MindCanvas\n");

        offsets[1] = output.Position;
        Write(output, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets[2] = output.Position;
        Write(output, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        offsets[3] = output.Position;
        Write(output,
            $"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {F(pageWidth)} {F(pageHeight)}] " +
            "/Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");

        offsets[4] = output.Position;
        Write(output,
            $"4 0 obj\n<< /Type /XObject /Subtype /Image /Width {width} /Height {height} " +
            $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {compressed.Length} >>\nstream\n");
        output.Write(compressed);
        Write(output, "\nendstream\nendobj\n");

        offsets[5] = output.Position;
        Write(output, $"5 0 obj\n<< /Length {content.Length} >>\nstream\n");
        output.Write(content);
        Write(output, "endstream\nendobj\n");

        var xref = output.Position;
        Write(output, "xref\n0 6\n0000000000 65535 f \n");
        for (var i = 1; i <= 5; i++)
            Write(output, $"{offsets[i]:D10} 00000 n \n");
        Write(output, $"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void Write(Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes);
    }
}
