using SkiaSharp;
using XIVLauncherProxy.Infrastructure.Authentication;
using ZXing;
using ZXing.Common;

namespace XIVLauncherProxy.Infrastructure.Tests;

public sealed class SkiaSharpQrCodeDecoderTests
{
    [Fact]
    public void AuthenticatorExportImageReturnsItsMigrationUri()
    {
        const string migration = "otpauth-migration://offline?data=CjEKCkhlbGxvId6tvu8SGEV4YW1wbGU6YWxpY2VAZ29vZ2xlLmNvbRoHRXhhbXBsZTAC";
        string path = Path.Combine(Path.GetTempPath(), $"xivlauncherproxy-{Guid.NewGuid():N}.png");
        try
        {
            var writer = new ZXing.SkiaSharp.BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions { Width = 500, Height = 500, Margin = 2 },
            };
            using SKBitmap image = writer.Write(migration);
            using (SKImage encoded = SKImage.FromBitmap(image))
            using (SKData data = encoded.Encode(SKEncodedImageFormat.Png, 100))
            using (FileStream file = File.Create(path))
                data.SaveTo(file);

            IReadOnlyList<string> values = new SkiaSharpQrCodeDecoder().Decode(path);

            Assert.Contains(migration, values);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
