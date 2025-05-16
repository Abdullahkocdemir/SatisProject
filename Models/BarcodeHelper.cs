using System.Drawing;
using ZXing;
using ZXing.Windows.Compatibility;

public static class BarcodeHelper
{
    public static Bitmap GenerateBarcodeBitmap(string text)
    {
        var barcodeWriter = new BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new ZXing.Common.EncodingOptions
            {
                Height = 100,
                Width = 300,
                Margin = 10
            }
        };

        return barcodeWriter.Write(text);
    }
}
