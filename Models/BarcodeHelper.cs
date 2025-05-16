using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.SkiaSharp.Rendering;

public static class BarcodeHelper
{
    public static byte[] GenerateBarcodeImage(string text, int width = 300, int height = 100)
    {
        var writer = new BarcodeWriter<SKBitmap>
        {
            Format = BarcodeFormat.CODE_128,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 10
            },
            Renderer = new SKBitmapRenderer()
        };

        using var bitmap = writer.Write(text);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
