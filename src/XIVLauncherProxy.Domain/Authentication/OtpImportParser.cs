using System.Text;

namespace XIVLauncherProxy.Domain.Authentication;

public static class OtpImportParser
{
    public static IReadOnlyList<OtpImportEntry> Parse(string value)
    {
        string input = value.Trim();
        if (input.StartsWith("otpauth-migration://", StringComparison.OrdinalIgnoreCase))
            return ParseGoogleMigration(input);
        if (input.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
            return [ParseStandard(input)];
        throw new FormatException("OtpImportFormatUnsupported");
    }

    private static OtpImportEntry ParseStandard(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || !uri.Host.Equals("totp", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("TotpUriInvalid");
        Dictionary<string, string> query = ReadQuery(uri.Query);
        if (!query.TryGetValue("secret", out string? secret))
            throw new FormatException("TotpSecretMissing");
        string name = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        query.TryGetValue("issuer", out string? issuer);
        return new OtpImportEntry(Base32Secret.Normalize(secret), name, issuer ?? string.Empty);
    }

    private static IReadOnlyList<OtpImportEntry> ParseGoogleMigration(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
            throw new FormatException("GoogleMigrationInvalid");
        Dictionary<string, string> query = ReadQuery(uri.Query);
        if (!query.TryGetValue("data", out string? encoded) || string.IsNullOrWhiteSpace(encoded))
            throw new FormatException("GoogleMigrationInvalid");
        byte[] payload;
        try
        {
            string base64 = encoded.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight((base64.Length + 3) / 4 * 4, '=');
            payload = Convert.FromBase64String(base64);
        }
        catch (FormatException exception)
        {
            throw new FormatException("GoogleMigrationInvalid", exception);
        }

        var entries = new List<OtpImportEntry>();
        var reader = new ProtobufReader(payload);
        while (reader.TryReadField(out int field, out int wireType))
        {
            if (field == 1 && wireType == 2)
                entries.Add(ParseParameters(reader.ReadLengthDelimited()));
            else
                reader.Skip(wireType);
        }
        if (entries.Count == 0)
            throw new FormatException("GoogleMigrationEmpty");
        return entries;
    }

    private static OtpImportEntry ParseParameters(ReadOnlySpan<byte> payload)
    {
        byte[] secret = [];
        string name = string.Empty;
        string issuer = string.Empty;
        ulong algorithm = 1;
        ulong digits = 1;
        ulong type = 2;
        var reader = new ProtobufReader(payload);
        while (reader.TryReadField(out int field, out int wireType))
        {
            switch (field)
            {
                case 1 when wireType == 2: secret = reader.ReadLengthDelimited().ToArray(); break;
                case 2 when wireType == 2: name = Encoding.UTF8.GetString(reader.ReadLengthDelimited()); break;
                case 3 when wireType == 2: issuer = Encoding.UTF8.GetString(reader.ReadLengthDelimited()); break;
                case 4 when wireType == 0: algorithm = reader.ReadVarint(); break;
                case 5 when wireType == 0: digits = reader.ReadVarint(); break;
                case 6 when wireType == 0: type = reader.ReadVarint(); break;
                default: reader.Skip(wireType); break;
            }
        }
        if (secret.Length == 0) throw new FormatException("TotpSecretMissing");
        if (algorithm is not (0 or 1)) throw new FormatException("TotpAlgorithmUnsupported");
        if (digits is not (0 or 1)) throw new FormatException("TotpDigitsUnsupported");
        if (type != 2) throw new FormatException("OtpTypeUnsupported");
        return new OtpImportEntry(Base32Secret.Encode(secret), name, issuer);
    }

    private static Dictionary<string, string> ReadQuery(string query) => query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split('=', 2))
        .ToDictionary(
            part => Uri.UnescapeDataString(part[0]),
            part => part.Length == 2 ? Uri.UnescapeDataString(part[1]) : string.Empty,
            StringComparer.OrdinalIgnoreCase);

    private ref struct ProtobufReader(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> data = data;
        private int offset;

        public bool TryReadField(out int field, out int wireType)
        {
            if (offset >= data.Length) { field = 0; wireType = 0; return false; }
            ulong tag = ReadVarint();
            field = checked((int)(tag >> 3));
            wireType = (int)(tag & 7);
            if (field == 0) throw new FormatException("GoogleMigrationInvalid");
            return true;
        }

        public ulong ReadVarint()
        {
            ulong value = 0;
            for (int shift = 0; shift < 64; shift += 7)
            {
                if (offset >= data.Length) throw new FormatException("GoogleMigrationInvalid");
                byte item = data[offset++];
                value |= (ulong)(item & 0x7f) << shift;
                if ((item & 0x80) == 0) return value;
            }
            throw new FormatException("GoogleMigrationInvalid");
        }

        public ReadOnlySpan<byte> ReadLengthDelimited()
        {
            int length = checked((int)ReadVarint());
            if (length < 0 || offset > data.Length - length)
                throw new FormatException("GoogleMigrationInvalid");
            ReadOnlySpan<byte> value = data.Slice(offset, length);
            offset += length;
            return value;
        }

        public void Skip(int wireType)
        {
            switch (wireType)
            {
                case 0: _ = ReadVarint(); break;
                case 1: Advance(8); break;
                case 2: _ = ReadLengthDelimited(); break;
                case 5: Advance(4); break;
                default: throw new FormatException("GoogleMigrationInvalid");
            }
        }

        private void Advance(int count)
        {
            if (offset > data.Length - count) throw new FormatException("GoogleMigrationInvalid");
            offset += count;
        }
    }
}
