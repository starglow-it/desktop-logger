using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamActivity.Contracts;

public sealed class AgentOptions
{
    public string ManagerUrl { get; set; } = "http://127.0.0.1:7443";
    public Guid DeviceId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public int HeartbeatSeconds { get; set; } = 30;
}

public sealed class HeartbeatWorker(
    HttpClient client,
    IOptions<AgentOptions> options,
    ILogger<HeartbeatWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (settings.DeviceId == Guid.Empty || string.IsNullOrWhiteSpace(settings.DeviceToken))
        {
            logger.LogWarning("Agent is not enrolled. Heartbeats are paused until device credentials are configured.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Clamp(settings.HeartbeatSeconds, 15, 300));
        using var timer = new PeriodicTimer(interval);
        do
        {
            await SendHeartbeatAsync(settings, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SendHeartbeatAsync(AgentOptions settings, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri(new Uri(settings.ManagerUrl.TrimEnd('/') + "/"), "api/heartbeats"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.DeviceToken);
            request.Content = JsonContent.Create(new HeartbeatRequest(
                settings.DeviceId,
                typeof(HeartbeatWorker).Assembly.GetName().Version?.ToString() ?? "0.1.0",
                DateTimeOffset.UtcNow,
                "Healthy",
                0));
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Heartbeat was rejected with status {StatusCode}.", response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Manager server is temporarily unavailable.");
        }
    }
}
