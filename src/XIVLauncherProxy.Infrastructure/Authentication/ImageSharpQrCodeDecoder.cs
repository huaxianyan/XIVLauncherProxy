using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp;

namespace XIVLauncherProxy.Infrastructure.Authentication;

public sealed class ImageSharpQrCodeDecoder : IQrCodeDecoder
{
    public IReadOnlyList<string> Decode(string imagePath)
    {
        try
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(imagePath);
            var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = [BarcodeFormat.QR_CODE],
                },
            };
            Result[] results = reader.DecodeMultiple(image) ?? [];
            if (results.Length == 0)
            {
                Result? result = reader.Decode(image);
                if (result is not null) results = [result];
            }
            return results.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnknownImageFormatException)
        {
            throw new ProxyException("QrImageReadFailed", exception);
        }
    }
}
