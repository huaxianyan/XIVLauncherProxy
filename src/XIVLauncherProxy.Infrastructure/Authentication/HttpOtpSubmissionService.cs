using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Authentication;

namespace XIVLauncherProxy.Infrastructure.Authentication;

public sealed class HttpOtpSubmissionService(
    ITotpCredentialStore credentials,
    TimeProvider timeProvider) : IOtpSubmissionService
{
    private static readonly Uri StatusUri = new("http://127.0.0.1:4646/");

    public async Task SubmitWhenRequestedAsync(string credentialKey, CancellationToken cancellationToken)
    {
        using var handler = new HttpClientHandler { UseProxy = false };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        if (!await WaitForRequestAsync(client, timeout.Token))
            return;

        DateTimeOffset now = timeProvider.GetUtcNow();
        TimeSpan delay = TotpGenerator.DelayUntilFreshCode(now);
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, timeProvider, timeout.Token);
        string code = TotpGenerator.Generate(credentials.Get(credentialKey), timeProvider.GetUtcNow());
        try
        {
            using HttpResponseMessage response = await client.GetAsync(
                new Uri($"http://127.0.0.1:4646/ffxivlauncher/{code}"), timeout.Token);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new ProxyException("OtpSubmitFailed", exception);
        }
    }

    private static async Task<bool> WaitForRequestAsync(HttpClient client, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    OtpStatus? status = await client.GetFromJsonAsync<OtpStatus>(StatusUri, cancellationToken);
                    if (status?.App == "XIVLauncher")
                        return true;
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        return false;
    }

    private sealed class OtpStatus
    {
        [JsonPropertyName("app")]
        public string? App { get; init; }
    }
}
