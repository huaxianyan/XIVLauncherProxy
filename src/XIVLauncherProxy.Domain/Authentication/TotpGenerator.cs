using System.Security.Cryptography;

namespace XIVLauncherProxy.Domain.Authentication;

public static class TotpGenerator
{
    private const int PeriodSeconds = 30;

    public static string Generate(string base32Secret, DateTimeOffset? timestamp = null)
    {
        byte[] secret = Base32Secret.Decode(base32Secret);
        long counter = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / PeriodSeconds;
        Span<byte> counterBytes = stackalloc byte[8];
        for (int index = counterBytes.Length - 1; index >= 0; index--)
        {
            counterBytes[index] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        byte[] hash = hmac.ComputeHash(counterBytes.ToArray());
        int offset = hash[^1] & 0x0f;
        int value = ((hash[offset] & 0x7f) << 24)
                    | (hash[offset + 1] << 16)
                    | (hash[offset + 2] << 8)
                    | hash[offset + 3];
        return (value % 1_000_000).ToString("D6");
    }

    public static TimeSpan DelayUntilFreshCode(DateTimeOffset? timestamp = null)
    {
        long seconds = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        int remaining = PeriodSeconds - (int)(seconds % PeriodSeconds);
        return remaining < 5 ? TimeSpan.FromSeconds(remaining + 1) : TimeSpan.Zero;
    }
}
