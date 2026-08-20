using System.Globalization;
using System.Resources;

namespace XIVLauncherProxy.Desktop.Localization;

public sealed class ResourceTextLocalizer : ITextLocalizer
{
    private static readonly ResourceManager Resources = new(
        "XIVLauncherProxy.Desktop.Localization.Strings",
        typeof(ResourceTextLocalizer).Assembly);
    public static ResourceTextLocalizer Instance { get; } = new();
    private ResourceTextLocalizer() { }
    public string this[string key] => Resources.GetString(key, CultureInfo.CurrentUICulture)
        ?? Resources.GetString(key, CultureInfo.InvariantCulture)
        ?? key;
    public string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, this[key], arguments);
}
