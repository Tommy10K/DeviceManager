using AutoMapper;
using DeviceManager.Application.DTOs;
using DeviceManager.Application.Exceptions;
using DeviceManager.Application.Interfaces;
using DeviceManager.Domain.Entities;
using DeviceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AuthController(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ITokenService tokenService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request)
    {
        ValidateRegisterRequest(request);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail))
        {
            throw new ConflictException($"User with email '{request.Email}' already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            Role = UserRole.User,
            Location = request.Location.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);

        var userDto = _mapper.Map<UserDto>(user);
        return StatusCode(StatusCodes.Status201Created, userDto);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        ValidateLoginRequest(request);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (user is null || !_passwordService.VerifyPassword(user, request.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Email, user.Role.ToString()));
    }

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Location))
        {
            throw new BadRequestException("Name, email, password, and location are required.");
        }

        if (request.Password.Length < 6)
        {
            throw new BadRequestException("Password must be at least 6 characters long.");
        }
    }

    private static void ValidateLoginRequest(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException("Email and password are required.");
        }
    }
}
