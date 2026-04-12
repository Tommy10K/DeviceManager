using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeviceManager.Application.DTOs;
using DeviceManager.Domain.Entities;
using DeviceManager.Domain.Enums;

namespace DeviceManager.IntegrationTests;

public sealed class DevicesControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public DevicesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_Login_ThenAccessProtectedEndpoint_Returns200()
    {
        var session = await RegisterAndLoginAsync("auth-flow-user");
        var authenticatedClient = CreateAuthenticatedClient(session.Token);

        var response = await authenticatedClient.GetAsync("/api/devices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var devices = await response.Content.ReadFromJsonAsync<List<DeviceDto>>();
        Assert.NotNull(devices);
    }

    [Fact]
    public async Task AccessProtectedEndpointWithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/devices");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest("Duplicate User", email, "Password123!", "Sydney");

        var first = await client.PostAsJsonAsync("/api/auth/register", request);
        var second = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task AccessAdminOnlyEndpointAsNonAdmin_Returns403()
    {
        var session = await RegisterAndLoginAsync("non-admin-user");
        var authenticatedClient = CreateAuthenticatedClient(session.Token);
        var request = BuildCreateRequest($"AUTH-{Guid.NewGuid():N}");

        var response = await authenticatedClient.PostAsJsonAsync("/api/devices", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignDeviceToSelf_ReturnsSuccess()
    {
        var session = await RegisterAndLoginAsync("assign-self-user");
        var deviceId = await SeedDeviceAsync($"ASSIGN-SELF-{Guid.NewGuid():N}", assignedUserId: null);
        var authenticatedClient = CreateAuthenticatedClient(session.Token);

        var response = await authenticatedClient.PostAsJsonAsync($"/api/devices/{deviceId}/assign", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedDevice = await response.Content.ReadFromJsonAsync<DeviceDto>();
        Assert.NotNull(updatedDevice);
        Assert.Equal(session.UserId, updatedDevice!.AssignedUserId);
    }

    [Fact]
    public async Task AssignAlreadyAssignedDevice_Returns409()
    {
        var ownerSession = await RegisterAndLoginAsync("device-owner-user");
        var otherSession = await RegisterAndLoginAsync("other-user");
        var deviceId = await SeedDeviceAsync($"ASSIGN-CONFLICT-{Guid.NewGuid():N}", ownerSession.UserId);
        var authenticatedClient = CreateAuthenticatedClient(otherSession.Token);

        var response = await authenticatedClient.PostAsJsonAsync($"/api/devices/{deviceId}/assign", new { });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UnassignDeviceAssignedToAnotherUser_Returns403()
    {
        var ownerSession = await RegisterAndLoginAsync("assigned-owner-user");
        var otherSession = await RegisterAndLoginAsync("different-user");
        var deviceId = await SeedDeviceAsync($"UNASSIGN-FORBIDDEN-{Guid.NewGuid():N}", ownerSession.UserId);
        var authenticatedClient = CreateAuthenticatedClient(otherSession.Token);

        var response = await authenticatedClient.PostAsJsonAsync($"/api/devices/{deviceId}/unassign", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateDescriptionFromSpecs_Returns200_WithNonEmptyText()
    {
        var session = await RegisterAndLoginAsync("description-specs-user");
        var authenticatedClient = CreateAuthenticatedClient(session.Token);

        var request = new GenerateDescriptionRequest(
            Name: "Pixel Pro",
            Manufacturer: "Google",
            OperatingSystem: "Android",
            Type: "Phone",
            RamAmount: "12GB",
            Processor: "Tensor G4");

        var response = await authenticatedClient.PostAsJsonAsync("/api/devices/generate-description", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var description = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(description));
        Assert.Contains("Pixel Pro", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateDescriptionForExistingDevice_Returns200_WithNonEmptyText()
    {
        var session = await RegisterAndLoginAsync("description-device-user");
        var deviceId = await SeedDeviceAsync($"AI-DETAIL-{Guid.NewGuid():N}", assignedUserId: null);
        var authenticatedClient = CreateAuthenticatedClient(session.Token);

        var response = await authenticatedClient.PostAsJsonAsync($"/api/devices/{deviceId}/generate-description", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var description = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(description));
        Assert.Contains("Integration Device", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateDescriptionFromSpecs_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var request = new GenerateDescriptionRequest(
            Name: "Galaxy S",
            Manufacturer: "Samsung",
            OperatingSystem: "Android",
            Type: "Phone",
            RamAmount: "8GB",
            Processor: "Snapdragon");

        var response = await client.PostAsJsonAsync("/api/devices/generate-description", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_ExactPhraseBoost_RanksExactNameFirst()
    {
        var session = await RegisterAndLoginAsync("search-exact-phrase-user");
        var authenticatedClient = CreateAuthenticatedClient(session.Token);

        await SeedSearchDevicesAsync(
            new SearchSeedDevice("SRCH-EXACT-1", "iPhone 15 Pro", "Apple", "A17 Pro", "8GB"),
            new SearchSeedDevice("SRCH-EXACT-2", "iPhone Budget", "Generic", "Series 15", "4GB"));

        var devices = await SearchDevicesAsync(authenticatedClient, "iPhone 15");

        Assert.NotEmpty(devices);
        Assert.Equal("SRCH-EXACT-1", devices[0].Tag);
    }

    [Fact]
    public async Task Search_AppleQuery_PrioritizesNameMatchOverManufacturerMatch()
    {
        var session = await RegisterAndLoginAsync("search-apple-user");
        var authenticatedClient = CreateAuthenticatedClient(session.Token);

        await SeedSearchDevicesAsync(
            new SearchSeedDevice("SRCH-APPLE-1", "Apple Enterprise Phone", "Contoso", "A17", "8GB"),
            new SearchSeedDevice("SRCH-APPLE-2", "Enterprise Phone", "Apple", "Snapdragon", "8GB"));

        var devices = await SearchDevicesAsync(authenticatedClient, "Apple");

        Assert.NotEmpty(devices);
        Assert.Equal("SRCH-APPLE-1", devices[0].Tag);
    }

    [Fact]
    public async Task Search_16GbSamsung_RanksMostRelevantDeviceFirst()
    {
        var session = await RegisterAndLoginAsync("search-samsung-user");
        var authenticatedClient = CreateAuthenticatedClient(session.Token);

        await SeedSearchDevicesAsync(
            new SearchSeedDevice("SRCH-SAM-16", "Business Galaxy", "Samsung", "Exynos", "16GB"),
            new SearchSeedDevice("SRCH-SAM-8", "Galaxy Lite", "Samsung", "Exynos", "8GB"),
            new SearchSeedDevice("SRCH-OTHER-16", "Office Tab", "Lenovo", "Snapdragon", "16GB"));

        var devices = await SearchDevicesAsync(authenticatedClient, "16GB Samsung");

        Assert.NotEmpty(devices);
        Assert.Equal("SRCH-SAM-16", devices[0].Tag);
    }

    [Fact]
    public async Task Search_MixedCaseAndExtraSpaces_ReturnsExpectedRanking()
    {
        var session = await RegisterAndLoginAsync("search-normalization-user");
        var authenticatedClient = CreateAuthenticatedClient(session.Token);

        await SeedSearchDevicesAsync(
            new SearchSeedDevice("SRCH-NORM-1", "Apple Enterprise Phone", "Contoso", "A17", "8GB"),
            new SearchSeedDevice("SRCH-NORM-2", "Enterprise Phone", "Apple", "Snapdragon", "8GB"));

        var devices = await SearchDevicesAsync(authenticatedClient, "   aPPLe   ");

        Assert.NotEmpty(devices);
        Assert.Equal("SRCH-NORM-1", devices[0].Tag);
    }

    [Fact]
    public async Task Search_NonExistentQuery_ReturnsEmptyList()
    {
        var session = await RegisterAndLoginAsync("search-empty-user");
        var authenticatedClient = CreateAuthenticatedClient(session.Token);

        await SeedSearchDevicesAsync(
            new SearchSeedDevice("SRCH-EMPTY-1", "Corporate Device", "Contoso", "Tensor", "8GB"));

        var devices = await SearchDevicesAsync(authenticatedClient, "nonexistent-zzzz");

        Assert.Empty(devices);
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<TestUserSession> RegisterAndLoginAsync(string emailPrefix)
    {
        var client = _factory.CreateClient();
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";

        var registerRequest = new RegisterRequest(
            Name: "Integration User",
            Email: email,
            Password: password,
            Location: "Melbourne");

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registeredUser = await registerResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(registeredUser);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(Email: email, Password: password));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse!.Token));

        return new TestUserSession(registeredUser!.Id, authResponse.Token);
    }

    private async Task<Guid> SeedDeviceAsync(string tag, Guid? assignedUserId)
    {
        var deviceId = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Devices.Add(new Device
            {
                Id = deviceId,
                Tag = tag,
                Name = "Integration Device",
                Manufacturer = "Integration Vendor",
                Type = DeviceType.Phone,
                OperatingSystem = "Android",
                OSVersion = "14",
                Processor = "Tensor",
                RamAmount = "8GB",
                Description = "integration",
                AssignedUserId = assignedUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        return deviceId;
    }

    private static CreateDeviceRequest BuildCreateRequest(string tag)
    {
        return new CreateDeviceRequest(
            Tag: tag,
            Name: "Admin Device",
            Manufacturer: "Admin Vendor",
            Type: DeviceType.Phone,
            OperatingSystem: "Android",
            OSVersion: "14",
            Processor: "Snapdragon",
            RamAmount: "12GB",
            Description: "auth integration",
            AssignedUserId: null);
    }

    private async Task<List<DeviceDto>> SearchDevicesAsync(HttpClient client, string query)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var response = await client.GetAsync($"/api/devices/search?q={encodedQuery}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var devices = await response.Content.ReadFromJsonAsync<List<DeviceDto>>();
        Assert.NotNull(devices);

        return devices!;
    }

    private async Task SeedSearchDevicesAsync(params SearchSeedDevice[] devices)
    {
        await _factory.ExecuteDbContextAsync(async dbContext =>
        {
            foreach (var device in devices)
            {
                dbContext.Devices.Add(new Device
                {
                    Id = Guid.NewGuid(),
                    Tag = device.Tag,
                    Name = device.Name,
                    Manufacturer = device.Manufacturer,
                    Type = DeviceType.Phone,
                    OperatingSystem = "Android",
                    OSVersion = "14",
                    Processor = device.Processor,
                    RamAmount = device.RamAmount,
                    Description = null,
                    AssignedUserId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync();
        });
    }

    private sealed record TestUserSession(Guid UserId, string Token);

    private sealed record SearchSeedDevice(
        string Tag,
        string Name,
        string Manufacturer,
        string Processor,
        string RamAmount);
}