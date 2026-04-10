using AutoMapper;
using DeviceManager.Application.DTOs;
using DeviceManager.Application.Exceptions;
using DeviceManager.Application.Interfaces;
using DeviceManager.Application.Services;
using DeviceManager.Application.Validators;
using DeviceManager.Domain.Entities;
using DeviceManager.Domain.Enums;
using Moq;

namespace DeviceManager.UnitTests;

public sealed class DeviceServiceTests
{
    [Fact]
    public async Task CreateDeviceAsync_ThrowsConflictException_WhenTagAlreadyExists()
    {
        var deviceRepository = new Mock<IDeviceRepository>();
        var userRepository = new Mock<IUserRepository>();
        var mapper = new Mock<IMapper>();

        deviceRepository
            .Setup(repository => repository.GetByTagAsync("DUP-001"))
            .ReturnsAsync(new Device { Id = Guid.NewGuid(), Tag = "DUP-001" });

        var service = new DeviceService(deviceRepository.Object, userRepository.Object, mapper.Object);
        var request = BuildCreateRequest(tag: "DUP-001", name: "Device A");

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateDeviceAsync(request));
    }

    [Fact]
    public async Task GetDeviceByIdAsync_ThrowsNotFoundException_WhenDeviceDoesNotExist()
    {
        var deviceRepository = new Mock<IDeviceRepository>();
        var userRepository = new Mock<IUserRepository>();
        var mapper = new Mock<IMapper>();

        deviceRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Device?)null);

        var service = new DeviceService(deviceRepository.Object, userRepository.Object, mapper.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetDeviceByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public void CreateDeviceRequestValidator_ReturnsError_WhenNameIsEmpty()
    {
        var validator = new CreateDeviceRequestValidator();
        var request = BuildCreateRequest(tag: "VAL-001", name: string.Empty);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Name");
    }

    private static CreateDeviceRequest BuildCreateRequest(string tag, string name)
    {
        return new CreateDeviceRequest(
            Tag: tag,
            Name: name,
            Manufacturer: "Test Manufacturer",
            Type: DeviceType.Phone,
            OperatingSystem: "Android",
            OSVersion: "14",
            Processor: "Test CPU",
            RamAmount: "8GB",
            Description: "Unit test request",
            AssignedUserId: null);
    }
}