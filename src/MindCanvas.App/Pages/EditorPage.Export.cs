using Microsoft.UI.Xaml.Media.Imaging;
using MindCanvas.Storage;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Storage;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    public async Task ExportPngAsync(StorageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        var restoreVirtualization = SuspendVirtualizationForFullCanvas();
        try
        {
            var (bitmap, pixels) = await RenderCanvasPixelsAsync();

            using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            stream.Size = 0;
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)bitmap.PixelWidth,
                (uint)bitmap.PixelHeight,
                96,
                96,
                pixels);
            await encoder.FlushAsync();
        }
        finally
        {
            ResumeVirtualizationAfterFullCanvas(restoreVirtualization);
        }
    }

    public async Task ExportPdfAsync(StorageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        var restoreVirtualization = SuspendVirtualizationForFullCanvas();
        try
        {
            var (bitmap, bgra) = await RenderCanvasPixelsAsync();
            var rgb = CompositePremultipliedBgraOnWhite(bgra);
            var pdf = new RgbPdfExporter().Export(bitmap.PixelWidth, bitmap.PixelHeight, rgb, 96);
            await FileIO.WriteBytesAsync(file, pdf);
        }
        finally
        {
            ResumeVirtualizationAfterFullCanvas(restoreVirtualization);
        }
    }

    private async Task<(RenderTargetBitmap Bitmap, byte[] Pixels)> RenderCanvasPixelsAsync()
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(MapCanvas);
        var buffer = await bitmap.GetPixelsAsync();
        CryptographicBuffer.CopyToByteArray(buffer, out var pixels);
        return (bitmap, pixels);
    }

    private static byte[] CompositePremultipliedBgraOnWhite(byte[] bgra)
    {
        if (bgra.Length % 4 != 0)
            throw new InvalidDataException("Unexpected BGRA pixel buffer length.");

        var rgb = new byte[checked(bgra.Length / 4 * 3)];
        for (int source = 0, target = 0; source < bgra.Length; source += 4, target += 3)
        {
            var blue = bgra[source];
            var green = bgra[source + 1];
            var red = bgra[source + 2];
            var alpha = bgra[source + 3];
            var white = 255 - alpha;
            rgb[target] = (byte)Math.Min(255, red + white);
            rgb[target + 1] = (byte)Math.Min(255, green + white);
            rgb[target + 2] = (byte)Math.Min(255, blue + white);
        }
        return rgb;
    }
}
