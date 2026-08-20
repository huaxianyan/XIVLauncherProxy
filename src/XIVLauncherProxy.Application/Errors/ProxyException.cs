namespace XIVLauncherProxy.Application.Errors;

public sealed class ProxyException(string code, Exception? innerException = null)
    : Exception(code, innerException)
{
    public string Code { get; } = code;
}
