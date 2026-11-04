using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HappyInventory.Application.DTOs.Auth;
using HappyInventory.Application.DTOs.Integrations;
using HappyInventory.Application.DTOs.Users;
using HappyInventory.Core.Enums;
using Xunit;

namespace HappyInventory.Tests;

public class ExceptionHandlingTests : IClassFixture<HappyInventoryApiFactory>
{
    private readonly HappyInventoryApiFactory _factory;

    public ExceptionHandlingTests(HappyInventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DomainExceptions_AreMappedToProblemResponses()
    {
        using var adminClient = await CreateAuthorizedClientAsync(
            "admin@happywarehouse.com",
            "AdminPassword123!");

        // NotFoundException -> 404
        var missingUser = await adminClient.GetAsync("/api/users/999999");
        Assert.Equal(HttpStatusCode.NotFound, missingUser.StatusCode);

        var notFoundProblem = await ReadProblemAsync(missingUser);
        Assert.Equal(404, notFoundProblem.Status);
        Assert.Contains(
            "not found",
            notFoundProblem.Detail,
            StringComparison.OrdinalIgnoreCase);

        // ConflictException -> 409
        var createUser = new CreateUserRequest
        {
            Username = "exception@test.com",
            FullName = "Exception Test",
            Role = UserRoles.Auditor,
            Password = "ExceptionPassword123!",
            IsActive = true,
        };

        var created = await adminClient.PostAsJsonAsync("/api/users", createUser);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var user = await created.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);

        var duplicate = await adminClient.PostAsJsonAsync("/api/users", createUser);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var conflictProblem = await ReadProblemAsync(duplicate);
        Assert.Equal(409, conflictProblem.Status);
        Assert.Contains(
            "already taken",
            conflictProblem.Detail,
            StringComparison.OrdinalIgnoreCase);

        // ValidationException -> 400
        var invalidInventoryUpdate = await adminClient.PostAsJsonAsync(
            "/api/inventory/updates",
            new InventoryUpdateRequest
            {
                WarehouseId = 1,
                SKUCode = "SKU-1",
                Quantity = 1,
            });

        Assert.Equal(HttpStatusCode.BadRequest, invalidInventoryUpdate.StatusCode);

        var validationProblem = await ReadProblemAsync(invalidInventoryUpdate);
        Assert.Equal(400, validationProblem.Status);
        Assert.Contains(
            "Idempotency-Key",
            validationProblem.Detail,
            StringComparison.OrdinalIgnoreCase);

        // A ValidationException thrown inside a service is mapped the same way.
        var invalidLogLevel = await adminClient.GetAsync("/api/system-logs?level=NotALevel");
        Assert.Equal(HttpStatusCode.BadRequest, invalidLogLevel.StatusCode);

        // AuthenticationException -> 401 with a stable code
        var disabled = await adminClient.PutAsJsonAsync(
            $"/api/users/{user!.Id}",
            new UpdateUserRequest
            {
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                Password = null,
                IsActive = false,
            });

        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);

        using var anonymousClient = _factory.CreateClient();
        var disabledLogin = await anonymousClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Username = createUser.Username,
                Password = createUser.Password,
            });

        Assert.Equal(HttpStatusCode.Unauthorized, disabledLogin.StatusCode);

        var authenticationProblem = await ReadProblemAsync(disabledLogin);
        Assert.Equal(401, authenticationProblem.Status);
        Assert.Equal("account_disabled", authenticationProblem.Code);
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

    private static async Task<ProblemResponse> ReadProblemAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        return problem!;
    }

    private sealed class ProblemResponse
    {
        public int Status { get; set; }
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public string? Code { get; set; }
    }
}
