using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HappyInventory.Application.DTOs.Auth;
using HappyInventory.Application.DTOs.Users;
using HappyInventory.Core.Enums;
using Xunit;

namespace HappyInventory.Tests;

public class RoleChangeTests : IClassFixture<HappyInventoryApiFactory>
{
    private const string AdminUsername = "admin@happywarehouse.com";
    private const string AdminPassword = "AdminPassword123!";

    private readonly HappyInventoryApiFactory _factory;

    public RoleChangeTests(HappyInventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Promotion_IsAppliedToAnExistingToken()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AdminUsername, AdminPassword);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest
            {
                Username = "promote@test.com",
                FullName = "Promote Test",
                Role = UserRoles.Auditor,
                Password = "PromotePassword123!",
                IsActive = true,
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);

        using var userClient = await CreateAuthorizedClientAsync(
            "promote@test.com",
            "PromotePassword123!");

        var beforePromotion = await userClient.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, beforePromotion.StatusCode);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/users/{user!.Id}",
            new UpdateUserRequest
            {
                Username = user.Username,
                FullName = user.FullName,
                Role = UserRoles.Admin,
                Password = null,
                IsActive = true,
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Reuse the token issued before the promotion. OnTokenValidated should
        // replace the role claim with the current database role.
        var afterPromotion = await userClient.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, afterPromotion.StatusCode);

        var refreshedProfile = await userClient.GetFromJsonAsync<UserDto>("/api/auth/me");
        Assert.NotNull(refreshedProfile);
        Assert.Equal(UserRoles.Admin, refreshedProfile!.Role);
    }

    [Fact]
    public async Task Demotion_IsAppliedToAnExistingToken()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AdminUsername, AdminPassword);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest
            {
                Username = "demote@test.com",
                FullName = "Demote Test",
                Role = UserRoles.Admin,
                Password = "DemotePassword123!",
                IsActive = true,
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);

        using var userClient = await CreateAuthorizedClientAsync(
            "demote@test.com",
            "DemotePassword123!");

        var beforeDemotion = await userClient.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, beforeDemotion.StatusCode);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/users/{user!.Id}",
            new UpdateUserRequest
            {
                Username = user.Username,
                FullName = user.FullName,
                Role = UserRoles.Auditor,
                Password = null,
                IsActive = true,
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var afterDemotion = await userClient.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, afterDemotion.StatusCode);
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
