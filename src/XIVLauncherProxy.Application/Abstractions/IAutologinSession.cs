namespace XIVLauncherProxy.Application.Abstractions;

public interface IAutologinSession : IDisposable;

public interface IAutologinSessionFactory
{
    IAutologinSession EnableTemporarily();
}
