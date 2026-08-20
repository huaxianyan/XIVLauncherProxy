namespace XIVLauncherProxy.Domain.Authentication;

public sealed record OtpImportEntry(string Secret, string Name, string Issuer)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Issuer)
        ? Name
        : string.IsNullOrWhiteSpace(Name) ? Issuer : $"{Issuer} · {Name}";
}
