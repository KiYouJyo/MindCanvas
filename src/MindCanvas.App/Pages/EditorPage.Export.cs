using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Storage;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    public async Task ExportPngAsync(StorageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(MapCanvas);
        var buffer = await bitmap.GetPixelsAsync();
        CryptographicBuffer.CopyToByteArray(buffer, out var pixels);

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
}
