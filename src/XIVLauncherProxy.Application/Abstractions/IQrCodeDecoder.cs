namespace XIVLauncherProxy.Application.Abstractions;

public interface IQrCodeDecoder
{
    IReadOnlyList<string> Decode(string imagePath);
}
