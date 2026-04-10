using System.Net;
using System.Net.Http.Json;
using DeviceManager.Application.DTOs;
using DeviceManager.Domain.Enums;

namespace DeviceManager.IntegrationTests;

public sealed class DevicesControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DevicesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoDevices()
    {
        var response = await _client.GetAsync("/api/devices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var devices = await response.Content.ReadFromJsonAsync<List<DeviceDto>>();
        Assert.NotNull(devices);
        Assert.Empty(devices!);
    }

    [Fact]
    public async Task Create_ReturnsCreatedDevice_WithValidData()
    {
        var request = BuildCreateRequest("IT-0001");

        var response = await _client.PostAsJsonAsync("/api/devices", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<DeviceDto>();
        Assert.NotNull(created);
        Assert.Equal("IT-0001", created!.Tag);
        Assert.Equal(request.Name, created.Name);
    }

    [Fact]
    public async Task Create_Returns409_WhenDuplicateTag()
    {
        var request = BuildCreateRequest("IT-DUPLICATE");

        var first = await _client.PostAsJsonAsync("/api/devices", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/devices", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_WhenDeviceNotFound()
    {
        var response = await _client.GetAsync($"/api/devices/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsUpdatedDevice_WithValidData()
    {
        var createRequest = BuildCreateRequest("IT-0002");
        var createResponse = await _client.PostAsJsonAsync("/api/devices", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<DeviceDto>();

        var updateRequest = new UpdateDeviceRequest(
            Tag: "IT-0002",
            Name: "Updated Device",
            Manufacturer: createRequest.Manufacturer,
            Type: createRequest.Type,
            OperatingSystem: createRequest.OperatingSystem,
            OSVersion: "15",
            Processor: createRequest.Processor,
            RamAmount: createRequest.RamAmount,
            Description: "updated",
            AssignedUserId: null);

        var updateResponse = await _client.PutAsJsonAsync($"/api/devices/{created!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<DeviceDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Device", updated!.Name);
        Assert.Equal("15", updated.OSVersion);
    }

    [Fact]
    public async Task Delete_Returns204_WhenDeviceExists()
    {
        var createRequest = BuildCreateRequest("IT-0003");
        var createResponse = await _client.PostAsJsonAsync("/api/devices", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<DeviceDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/devices/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Create_Returns400_WhenFieldsMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/devices", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static CreateDeviceRequest BuildCreateRequest(string tag)
    {
        return new CreateDeviceRequest(
            Tag: tag,
            Name: "Integration Device",
            Manufacturer: "Integration Vendor",
            Type: DeviceType.Tablet,
            OperatingSystem: "Android",
            OSVersion: "14",
            Processor: "Tensor",
            RamAmount: "8GB",
            Description: "integration-test",
            AssignedUserId: null);
    }
}