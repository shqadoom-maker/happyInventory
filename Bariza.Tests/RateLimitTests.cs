using System.Net;
using System.Net.Http.Json;
using Bariza.Application.DTOs.Auth;
using Xunit;

namespace Bariza.Tests;

public class RateLimitTests : IClassFixture<HappyInventoryApiFactory>
{
    private readonly HappyInventoryApiFactory _factory;

    public RateLimitTests(HappyInventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginRateLimit_IgnoresSpoofedRealIpAndClientIdHeaders()
    {
        using var client = _factory.CreateClient();

        HttpResponseMessage? lastResponse = null;

        for (var attempt = 0; attempt < 6; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new LoginRequest
                {
                    Username = "admin@happywarehouse.com",
                    Password = "WrongPassword123!",
                }),
            };

            // If the application trusted these headers, every request would
            // appear to come from a different client and the limit would never
            // be reached.
            request.Headers.TryAddWithoutValidation("X-Real-IP", $"203.0.113.{attempt + 1}");
            request.Headers.TryAddWithoutValidation("X-Client-Id", $"spoofed-client-{attempt}");

            lastResponse?.Dispose();
            lastResponse = await client.SendAsync(request);
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
        lastResponse.Dispose();
    }
}
