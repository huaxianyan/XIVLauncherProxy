namespace XIVLauncherProxy.Domain.Authentication;

public static class Base32Secret
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Normalize(string value)
    {
        string input = value.Trim();
        if (input.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
            input = ReadFromUri(input);

        string normalized = new(input
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .Select(char.ToUpperInvariant)
            .ToArray());
        normalized = normalized.TrimEnd('=');
        if (normalized.Length == 0)
            throw new FormatException("TotpSecretMissing");

        _ = Decode(normalized);
        return normalized;
    }

    public static byte[] Decode(string value)
    {
        var output = new List<byte>(value.Length * 5 / 8);
        int buffer = 0;
        int bits = 0;
        foreach (char character in value)
        {
            int digit = Alphabet.IndexOf(char.ToUpperInvariant(character));
            if (digit < 0)
                throw new FormatException("TotpSecretInvalidBase32");
            buffer = (buffer << 5) | digit;
            bits += 5;
            if (bits < 8)
                continue;
            bits -= 8;
            output.Add((byte)(buffer >> bits));
            buffer &= (1 << bits) - 1;
        }

        if (output.Count == 0)
            throw new FormatException("TotpSecretTooShort");
        return output.ToArray();
    }

    private static string ReadFromUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || !uri.Scheme.Equals("otpauth", StringComparison.OrdinalIgnoreCase)
            || !uri.Host.Equals("totp", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("TotpUriInvalid");

        Dictionary<string, string> parameters = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                part => Uri.UnescapeDataString(part[0]),
                part => part.Length == 2 ? Uri.UnescapeDataString(part[1].Replace('+', ' ')) : string.Empty,
                StringComparer.OrdinalIgnoreCase);

        if (parameters.TryGetValue("algorithm", out string? algorithm)
            && !algorithm.Equals("SHA1", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("TotpAlgorithmUnsupported");
        if (parameters.TryGetValue("digits", out string? digits) && digits != "6")
            throw new FormatException("TotpDigitsUnsupported");
        if (parameters.TryGetValue("period", out string? period) && period != "30")
            throw new FormatException("TotpPeriodUnsupported");
        if (!parameters.TryGetValue("secret", out string? secret) || string.IsNullOrWhiteSpace(secret))
            throw new FormatException("TotpSecretMissing");
        return secret;
    }
}
