using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bariza.Application.DTOs.Auth;
using Bariza.Application.DTOs.Users;
using Bariza.Core.Enums;
using Xunit;

namespace Bariza.Tests;

public class DeactivationTests : IClassFixture<HappyInventoryApiFactory>
{
    private readonly HappyInventoryApiFactory _factory;

    public DeactivationTests(HappyInventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeactivatedUser_ExistingToken_IsRejected()
    {
        using var adminClient = await CreateAuthorizedClientAsync(
            "admin@happywarehouse.com",
            "AdminPassword123!");

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest
            {
                Username = "deactivate@test.com",
                FullName = "Deactivate Test",
                Role = UserRoles.Auditor,
                Password = "DeactivatePassword123!",
                IsActive = true,
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);

        using var userClient = await CreateAuthorizedClientAsync(
            "deactivate@test.com",
            "DeactivatePassword123!");

        var beforeDeactivation = await userClient.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, beforeDeactivation.StatusCode);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/users/{user!.Id}",
            new UpdateUserRequest
            {
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                Password = null,
                IsActive = false,
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // The token itself is still within its lifetime, but OnTokenValidated
        // rejects the inactive user on the next request.
        var afterDeactivation = await userClient.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, afterDeactivation.StatusCode);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(string username, string password)
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Username = username, Password = password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.Token);

        return client;
    }
}
