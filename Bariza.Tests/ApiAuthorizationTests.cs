using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bariza.Application.DTOs.Auth;
using Bariza.Application.DTOs.Users;
using Bariza.Core.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Bariza.Tests;

public class ApiAuthorizationTests : IClassFixture<HappyInventoryApiFactory>
{
    private readonly HappyInventoryApiFactory _factory;

    public ApiAuthorizationTests(HappyInventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ThenDashboardAndAdminAuthorization_WorkWithoutPasswordChangeGate()
    {
        using var anonymousClient = _factory.CreateClient();

        var loginResponse = await anonymousClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Username = "admin@happywarehouse.com",
                Password = "AdminPassword123!",
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.NotEmpty(login!.Token);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.Token);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == "mustChangePassword");

        using var adminClient = CreateAuthorizedClient(login.Token);
        var adminDashboard = await adminClient.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, adminDashboard.StatusCode);

        var users = await adminClient.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);

        var createAuditor = await adminClient.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest
            {
                Username = "auditor@test.com",
                FullName = "Auditor Test",
                Role = UserRoles.Auditor,
                Password = "AuditorPassword123!",
                IsActive = true,
            });
        Assert.Equal(HttpStatusCode.Created, createAuditor.StatusCode);

        var auditorLoginResponse = await anonymousClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Username = "auditor@test.com",
                Password = "AuditorPassword123!",
            });
        Assert.Equal(HttpStatusCode.OK, auditorLoginResponse.StatusCode);

        var auditorLogin = await auditorLoginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(auditorLogin);

        using var auditorClient = CreateAuthorizedClient(auditorLogin!.Token);
        var auditorDashboard = await auditorClient.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, auditorDashboard.StatusCode);

        var auditorUsers = await auditorClient.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, auditorUsers.StatusCode);
    }

    [Fact]
    public async Task Dashboard_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateAuthorizedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class HappyInventoryApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"happy-api-test-{Guid.NewGuid()}.db");

    private readonly Dictionary<string, string?> _originalEnvironment = new();

    public HappyInventoryApiFactory()
    {
        SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_databasePath}");
        SetEnvironmentVariable("Jwt__Issuer", "happyInventory");
        SetEnvironmentVariable("Jwt__Audience", "Bariza.Client");
        SetEnvironmentVariable("Jwt__Key", new string('T', 32));
        SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
        SetEnvironmentVariable("Seed__AdminUsername", "admin@happywarehouse.com");
        SetEnvironmentVariable("Seed__AdminPassword", "AdminPassword123!");
        SetEnvironmentVariable("Seed__AdminFullName", "System Administrator");
        SetEnvironmentVariable("Logging__Database__Enabled", "false");
        SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:4200");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        foreach (var (key, value) in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch (IOException)
        {
            // SQLite may still hold the file briefly; the OS temp folder will
            // clean it up later without affecting the test result.
        }
    }

    private void SetEnvironmentVariable(string key, string value)
    {
        _originalEnvironment[key] = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }
}
