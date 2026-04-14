using Microsoft.Extensions.Configuration;
using PatientTracker.Application.Common;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using BCrypt.Net;

namespace PatientTracker.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        // Create new user
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user = await _userRepository.CreateAsync(user);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.Profile?.FullName,
            CreatedAt = user.CreatedAt
        });

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpiration"]!)),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateRefreshTokenAsync(refreshTokenEntity);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.Profile?.FullName,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new BusinessException(ErrorCodes.InvalidCredentials);
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.Profile?.FullName,
            CreatedAt = user.CreatedAt
        });

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpiration"]!)),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateRefreshTokenAsync(refreshTokenEntity);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.Profile?.FullName,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshToken = await _userRepository.GetRefreshTokenAsync(request.RefreshToken);
        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.IsUsed || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid refresh token");
        }

        var principal = _jwtService.GetPrincipalFromExpiredToken(refreshToken.Token);
        if (principal == null)
        {
            throw new InvalidOperationException("Invalid access token");
        }

        var userId = int.Parse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Mark the refresh token as used
        refreshToken.IsUsed = true;
        await _userRepository.UpdateRefreshTokenAsync(refreshToken);

        // Generate new tokens
        var accessToken = _jwtService.GenerateAccessToken(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.Profile?.FullName,
            CreatedAt = user.CreatedAt
        });

        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpiration"]!)),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateRefreshTokenAsync(newRefreshTokenEntity);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.Profile?.FullName,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        return await _userRepository.RevokeRefreshTokenAsync(refreshToken);
    }
}
