using XIVLauncherProxy.Domain.Authentication;

namespace XIVLauncherProxy.Domain.Tests;

public sealed class TotpGeneratorTests
{
    [Fact]
    public void BitwardenSecretAtKnownTimeProducesExpectedCode()
    {
        const string secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(59);

        string code = TotpGenerator.Generate(secret, timestamp);

        Assert.Equal("287082", code);
    }

    [Fact]
    public void GoogleAuthenticatorMigrationExportImportsItsAccount()
    {
        const string migration = "otpauth-migration://offline?data=CjEKCkhlbGxvId6tvu8SGEV4YW1wbGU6YWxpY2VAZ29vZ2xlLmNvbRoHRXhhbXBsZTAC";

        OtpImportEntry entry = Assert.Single(OtpImportParser.Parse(migration));

        Assert.Equal("JBSWY3DPEHPK3PXP", entry.Secret);
        Assert.Equal("Example:alice@google.com", entry.Name);
        Assert.Equal("Example", entry.Issuer);
    }

    [Fact]
    public void StandardOtpAuthUriImportsItsBase32Secret()
    {
        const string secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        string uri = $"otpauth://totp/Square%20Enix:test?secret={secret}&algorithm=SHA1&digits=6&period=30";

        string normalized = Base32Secret.Normalize(uri);

        Assert.Equal(secret, normalized);
    }
}
