using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using PatientTracker.Application.Common;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Application.Resources;
using PatientTracker.Domain.Entities;
using BCrypt.Net;

namespace PatientTracker.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly IPasswordResetService _passwordResetService;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, IConfiguration configuration, IUnitOfWork unitOfWork, IStringLocalizer<ErrorMessages> localizer, IPasswordResetService passwordResetService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
        _passwordResetService = passwordResetService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new BusinessException(ErrorCodes.UserAlreadyExists, _localizer["UserAlreadyExists"]);
        }

        // Create new user
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _userRepository.Add(user);
        await _unitOfWork.CompleteAsync(); // Save User first to get the ID

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.Profile?.FullName,
            CreatedAt = user.CreatedAt
        });

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpirationDays = double.Parse(_configuration["Jwt:RefreshTokenExpiration"]!, System.Globalization.CultureInfo.InvariantCulture);
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.CreateRefreshTokenAsync(refreshTokenEntity);
        await _unitOfWork.CompleteAsync(); // Save RefreshToken

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

        if (user == null)
        {
            throw new BusinessException(ErrorCodes.InvalidCredentials, _localizer["InvalidCredentials"]);
        }

        // Check if account is locked out
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            var remaining = (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
            throw new BusinessException(ErrorCodes.AccessDenied, string.Format(_localizer["AccountLockedOut"], remaining));
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginAttempts = 0;
            }
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _unitOfWork.CompleteAsync();
            throw new BusinessException(ErrorCodes.InvalidCredentials, _localizer["InvalidCredentials"]);
        }

        // Reset failed attempts on successful login
        if (user.FailedLoginAttempts > 0 || user.LockoutEnd.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
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
        var refreshTokenExpirationDays = double.Parse(_configuration["Jwt:RefreshTokenExpiration"]!, System.Globalization.CultureInfo.InvariantCulture);
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.CreateRefreshTokenAsync(refreshTokenEntity);

        await _unitOfWork.CompleteAsync();

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
            throw new BusinessException(ErrorCodes.InvalidToken, _localizer["InvalidToken"]);
        }

        var principal = _jwtService.GetPrincipalFromExpiredToken(refreshToken.Token);
        if (principal == null)
        {
            throw new BusinessException(ErrorCodes.InvalidToken, _localizer["InvalidToken"]);
        }

        var userId = int.Parse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new BusinessException(ErrorCodes.UserNotFound, _localizer["UserNotFound"]);
        }

        // Mark the refresh token as used
        refreshToken.IsUsed = true;
        _userRepository.UpdateRefreshTokenAsync(refreshToken);

        // Generate new tokens
        var accessToken = _jwtService.GenerateAccessToken(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.Profile?.FullName,
            CreatedAt = user.CreatedAt
        });

        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpirationDays = double.Parse(_configuration["Jwt:RefreshTokenExpiration"]!, System.Globalization.CultureInfo.InvariantCulture);
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.CreateRefreshTokenAsync(newRefreshTokenEntity);

        await _unitOfWork.CompleteAsync();

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

    public async Task<string> RequestPasswordResetAsync(string email)
    {
        return await _passwordResetService.GeneratePasswordResetTokenAsync(email);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        return await _passwordResetService.ResetPasswordAsync(request);
    }
}
