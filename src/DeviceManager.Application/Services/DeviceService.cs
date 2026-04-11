using AutoMapper;
using DeviceManager.Application.DTOs;
using DeviceManager.Application.Exceptions;
using DeviceManager.Application.Interfaces;
using DeviceManager.Domain.Entities;

namespace DeviceManager.Application.Services;

public sealed class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public DeviceService(
        IDeviceRepository deviceRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _deviceRepository = deviceRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<List<DeviceDto>> GetAllDevicesAsync()
    {
        var devices = await _deviceRepository.GetAllAsync();
        return _mapper.Map<List<DeviceDto>>(devices);
    }

    public async Task<DeviceDto> GetDeviceByIdAsync(Guid id)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        if (device is null)
        {
            throw new NotFoundException($"Device with id '{id}' was not found.");
        }

        return _mapper.Map<DeviceDto>(device);
    }

    public async Task<DeviceDto> CreateDeviceAsync(CreateDeviceRequest request)
    {
        await EnsureUniqueTagAsync(request.Tag);

        var assignedUser = await ResolveAssignedUserAsync(request.AssignedUserId);

        var device = _mapper.Map<Device>(request);
        var now = DateTime.UtcNow;
        device.Id = Guid.NewGuid();
        device.CreatedAt = now;
        device.UpdatedAt = now;
        device.AssignedUser = assignedUser;

        var createdDevice = await _deviceRepository.AddAsync(device);
        createdDevice.AssignedUser ??= assignedUser;

        return _mapper.Map<DeviceDto>(createdDevice);
    }

    public async Task<DeviceDto> UpdateDeviceAsync(Guid id, UpdateDeviceRequest request)
    {
        var existingDevice = await _deviceRepository.GetByIdAsync(id);
        if (existingDevice is null)
        {
            throw new NotFoundException($"Device with id '{id}' was not found.");
        }

        await EnsureUniqueTagAsync(request.Tag, id);

        var assignedUser = await ResolveAssignedUserAsync(request.AssignedUserId);

        _mapper.Map(request, existingDevice);
        existingDevice.AssignedUser = assignedUser;
        existingDevice.UpdatedAt = DateTime.UtcNow;

        var updatedDevice = await _deviceRepository.UpdateAsync(existingDevice);
        updatedDevice.AssignedUser ??= assignedUser;

        return _mapper.Map<DeviceDto>(updatedDevice);
    }

    public async Task DeleteDeviceAsync(Guid id)
    {
        var existingDevice = await _deviceRepository.GetByIdAsync(id);
        if (existingDevice is null)
        {
            throw new NotFoundException($"Device with id '{id}' was not found.");
        }

        await _deviceRepository.DeleteAsync(id);
    }

    public async Task<DeviceDto> AssignDeviceToUserAsync(Guid deviceId, Guid userId)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId);
        if (device is null)
        {
            throw new NotFoundException($"Device with id '{deviceId}' was not found.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            throw new NotFoundException($"User with id '{userId}' was not found.");
        }

        if (device.AssignedUserId.HasValue)
        {
            if (device.AssignedUserId.Value == userId)
            {
                throw new BadRequestException("Device is already assigned to the current user.");
            }

            throw new ConflictException("Device is already assigned to another user.");
        }

        device.AssignedUserId = userId;
        device.AssignedUser = user;
        device.UpdatedAt = DateTime.UtcNow;

        var updatedDevice = await _deviceRepository.UpdateAsync(device);
        updatedDevice.AssignedUser ??= user;

        return _mapper.Map<DeviceDto>(updatedDevice);
    }

    public async Task<DeviceDto> UnassignDeviceFromUserAsync(Guid deviceId, Guid userId)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId);
        if (device is null)
        {
            throw new NotFoundException($"Device with id '{deviceId}' was not found.");
        }

        if (!device.AssignedUserId.HasValue)
        {
            throw new BadRequestException("Device is not currently assigned.");
        }

        if (device.AssignedUserId.Value != userId)
        {
            throw new ForbiddenException("You can only unassign devices assigned to you.");
        }

        device.AssignedUserId = null;
        device.AssignedUser = null;
        device.UpdatedAt = DateTime.UtcNow;

        var updatedDevice = await _deviceRepository.UpdateAsync(device);
        return _mapper.Map<DeviceDto>(updatedDevice);
    }

    private async Task EnsureUniqueTagAsync(string tag, Guid? currentDeviceId = null)
    {
        var existingDeviceWithTag = await _deviceRepository.GetByTagAsync(tag);
        if (existingDeviceWithTag is null)
        {
            return;
        }

        if (currentDeviceId.HasValue && existingDeviceWithTag.Id == currentDeviceId.Value)
        {
            return;
        }

        throw new ConflictException($"A device with tag '{tag}' already exists.");
    }

    private async Task<User?> ResolveAssignedUserAsync(Guid? assignedUserId)
    {
        if (!assignedUserId.HasValue)
        {
            return null;
        }

        if (assignedUserId.Value == Guid.Empty)
        {
            throw new BadRequestException("Assigned user id cannot be empty.");
        }

        var user = await _userRepository.GetByIdAsync(assignedUserId.Value);
        if (user is null)
        {
            throw new BadRequestException($"Assigned user with id '{assignedUserId}' was not found.");
        }

        return user;
    }
}