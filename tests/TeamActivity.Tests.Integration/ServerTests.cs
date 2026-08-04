using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TeamActivity.Tests.Integration;

public sealed class ServerTests : IDisposable
{
    private readonly string dataRoot = Path.Combine(Path.GetTempPath(), $"teamactivity-integration-{Guid.NewGuid():N}");
    private readonly WebApplicationFactory<Program> factory;

    public ServerTests()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DataRoot"] = dataRoot
                })));
    }

    [Fact]
    public async Task Health_endpoint_is_available()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unknown_raw_key_field_is_rejected_before_storage()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent(
            "{\"deviceId\":\"00000000-0000-0000-0000-000000000001\",\"startedAtUtc\":\"2026-08-04T12:00:00Z\",\"endedAtUtc\":\"2026-08-04T12:01:00Z\",\"state\":\"Active\",\"keyboardEventCount\":1,\"mouseEventCount\":0,\"mouseDistancePixels\":0,\"key\":\"A\"}",
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync("/api/activity-buckets", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        factory.Dispose();
        if (Directory.Exists(dataRoot)) Directory.Delete(dataRoot, recursive: true);
    }
}
