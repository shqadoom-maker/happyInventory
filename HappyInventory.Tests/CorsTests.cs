using System.Net;
using Xunit;

namespace HappyInventory.Tests;

public class CorsTests : IClassFixture<HappyInventoryApiFactory>
{
    private readonly HappyInventoryApiFactory _factory;

    public CorsTests(HappyInventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preflight_FromConfiguredOrigin_IsAllowed()
    {
        using var client = _factory.CreateClient();
        using var request = CreatePreflightRequest("http://localhost:4200");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Contains("http://localhost:4200", values);
        Assert.True(response.Headers.TryGetValues(
            "Access-Control-Allow-Credentials",
            out var credentials));
        Assert.Contains("true", credentials);
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_IsRejected()
    {
        using var client = _factory.CreateClient();
        using var request = CreatePreflightRequest("https://evil.example");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/dashboard");
        request.Headers.TryAddWithoutValidation("Origin", origin);
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "GET");
        request.Headers.TryAddWithoutValidation(
            "Access-Control-Request-Headers",
            "authorization");
        return request;
    }
}
