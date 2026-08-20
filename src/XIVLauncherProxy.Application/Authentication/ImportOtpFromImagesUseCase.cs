using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Authentication;

namespace XIVLauncherProxy.Application.Authentication;

public sealed class ImportOtpFromImagesUseCase(IQrCodeDecoder decoder)
{
    public IReadOnlyList<OtpImportEntry> Execute(IReadOnlyList<string> imagePaths)
    {
        var entries = new List<OtpImportEntry>();
        foreach (string path in imagePaths)
        {
            IReadOnlyList<string> values = decoder.Decode(path);
            foreach (string value in values)
                entries.AddRange(OtpImportParser.Parse(value));
        }
        OtpImportEntry[] distinct = entries.DistinctBy(x => x.Secret).ToArray();
        if (distinct.Length == 0)
            throw new ProxyException("QrCodeNotFound");
        return distinct;
    }
}
