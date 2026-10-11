using SkiaSharp;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;
using ZXing;
using ZXing.Common;

namespace XIVLauncherProxy.Infrastructure.Authentication;

public sealed class SkiaSharpQrCodeDecoder : IQrCodeDecoder
{
    public IReadOnlyList<string> Decode(string imagePath)
    {
        try
        {
            using SKBitmap? bitmap = SKBitmap.Decode(imagePath);
            if (bitmap is null)
                throw new ProxyException("QrImageReadFailed");
            var reader = new ZXing.SkiaSharp.BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = [BarcodeFormat.QR_CODE],
                },
            };
            Result[] results = reader.DecodeMultiple(bitmap);
            return results.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }
        catch (Exception exception) when (exception is not ProxyException)
        {
            throw new ProxyException("QrImageReadFailed", exception);
        }
    }
}
