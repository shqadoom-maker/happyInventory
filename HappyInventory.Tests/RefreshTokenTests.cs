using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HappyInventory.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HappyInventory.Tests;

public class RefreshTokenTests : IClassFixture<HappyInventoryApiFactory>
{
    private const string RefreshCookieName = "happy_inventory_refresh";
    private const string AdminUsername = "admin@happywarehouse.com";
    private const string AdminPassword = "AdminPassword123!";

    private readonly HappyInventoryApiFactory _factory;

    public RefreshTokenTests(HappyInventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_SetsHttpOnlyCookie_AndRefreshRotatesTheToken()
    {
        using var client = CreateCookieClient();

        var loginResponse = await LoginAsync(client);
        var loginCookie = GetRefreshCookie(loginResponse);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);
        Assert.Contains($"{RefreshCookieName}=", loginCookie);
        Assert.Contains("httponly", loginCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", loginCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", loginCookie, StringComparison.OrdinalIgnoreCase);

        var refreshResponse = await client.PostAsync("/api/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var rotatedCookie = GetRefreshCookie(refreshResponse);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(refreshed);
        Assert.NotEqual(login!.Token, refreshed!.Token);
        Assert.NotEqual(CookieValue(loginCookie), CookieValue(rotatedCookie));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshed.Token);

        var dashboard = await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
    }

    [Fact]
    public async Task ReusingARotatedRefreshToken_IsRejected_AndRevokesTheReplacement()
    {
        using var client = CreateCookieClient();

        var loginResponse = await LoginAsync(client);
        var originalCookie = GetRefreshCookie(loginResponse);

        var refreshResponse = await client.PostAsync("/api/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var replacementCookie = GetRefreshCookie(refreshResponse);

        using var replayClient = CreateRawClient();

        var replay = await PostWithCookieAsync(replayClient, originalCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(string.Empty, CookieValue(GetRefreshCookie(replay)));

        // Reuse of a rotated token is treated as a possible theft, so the
        // replacement issued by the legitimate request is revoked as well.
        var replacementAttempt = await PostWithCookieAsync(replayClient, replacementCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, replacementAttempt.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken_AndClearsTheCookie()
    {
        using var client = CreateCookieClient();

        var loginResponse = await LoginAsync(client);
        var cookie = GetRefreshCookie(loginResponse);

        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var deletedCookie = GetRefreshCookie(logoutResponse);
        Assert.Equal(string.Empty, CookieValue(deletedCookie));

        using var replayClient = CreateRawClient();
        var replay = await PostWithCookieAsync(replayClient, cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_IsRejected()
    {
        using var client = CreateRawClient();

        var response = await client.PostAsync("/api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateCookieClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

    private HttpClient CreateRawClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Username = AdminUsername,
                Password = AdminPassword,
                RememberMe = true,
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response;
    }

    private static Task<HttpResponseMessage> PostWithCookieAsync(
        HttpClient client,
        string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.TryAddWithoutValidation("Cookie", CookiePair(cookie));

        return client.SendAsync(request);
    }

    private static string GetRefreshCookie(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values));
        Assert.NotNull(values);

        var cookie = values!.FirstOrDefault(value =>
            value.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal));

        Assert.NotNull(cookie);
        return cookie!;
    }

    private static string CookiePair(string setCookieHeader)
    {
        var separator = setCookieHeader.IndexOf(';');
        return separator >= 0 ? setCookieHeader[..separator] : setCookieHeader;
    }

    private static string CookieValue(string setCookieHeader)
    {
        var pair = CookiePair(setCookieHeader);
        return pair[(pair.IndexOf('=') + 1)..];
    }
}
