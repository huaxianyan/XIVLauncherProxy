namespace XIVLauncherProxy.Application.Errors;

public static class ErrorCodes
{
    public static string From(Exception exception) => exception switch
    {
        ProxyException proxy => proxy.Code,
        FormatException format => format.Message,
        _ => "UnexpectedError",
    };
}
