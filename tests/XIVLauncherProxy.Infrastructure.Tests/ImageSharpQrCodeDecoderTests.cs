using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using XIVLauncherProxy.Infrastructure.Authentication;
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp;

namespace XIVLauncherProxy.Infrastructure.Tests;

public sealed class ImageSharpQrCodeDecoderTests
{
    [Fact]
    public void AuthenticatorExportImageReturnsItsMigrationUri()
    {
        const string migration = "otpauth-migration://offline?data=CjEKCkhlbGxvId6tvu8SGEV4YW1wbGU6YWxpY2VAZ29vZ2xlLmNvbRoHRXhhbXBsZTAC";
        string path = Path.Combine(Path.GetTempPath(), $"xivlauncherproxy-{Guid.NewGuid():N}.png");
        try
        {
            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions { Width = 500, Height = 500, Margin = 2 },
            };
            using Image<Rgba32> image = writer.Write(migration);
            image.SaveAsPng(path);

            IReadOnlyList<string> values = new ImageSharpQrCodeDecoder().Decode(path);

            Assert.Contains(migration, values);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
