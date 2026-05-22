using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PatientTracker.Application.Common;
using PatientTracker.Application.Resources;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AppValidationException = PatientTracker.Application.Common.ValidationException;

namespace PatientTracker.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly IUnitOfWork _unitOfWork;

    public PasswordResetService(
        IUserRepository userRepository,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<PasswordResetService> logger,
        IStringLocalizer<ErrorMessages> localizer,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            // Don't reveal that the user doesn't exist
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                { "Email", new string[] { _localizer["PasswordResetEmailSent"] } }
            });
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("type", "password-reset")
            }),
            Expires = DateTime.UtcNow.AddHours(1), // Token expires in 1 hour
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Generate reset link (you might want to configure the base URL in appsettings)
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:8081";
        var resetLink = $"{baseUrl}/reset-password?token={tokenString}";

        try
        {
            await _emailService.SendPasswordResetEmailAsync(email, resetLink);
            _logger.LogInformation("Password reset email sent to {Email}", email);
            return "Password reset email sent";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            throw new InvalidOperationException("Failed to send password reset email");
        }
    }

    public async Task<bool> ValidateResetTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            // Check if this is a password reset token
            var typeClaim = principal.FindFirst("type")?.Value;
            if (typeClaim != "password-reset")
            {
                return false;
            }

            // Check if user still exists
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                var user = await _userRepository.GetByEmailAsync(email);
                return user != null;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid password reset token provided");
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        // Validation
        if (string.IsNullOrEmpty(request.Token))
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                { "Token", new[] { "Token is required" } }
            });
        }

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                { "NewPassword", new[] { "New password must be at least 6 characters long" } }
            });
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                { "ConfirmPassword", new[] { "Passwords do not match" } }
            });
        }

        if (!await ValidateResetTokenAsync(request.Token))
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                { "Token", new[] { "Invalid or expired reset token" } }
            });
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(request.Token, validationParameters, out SecurityToken validatedToken);
            var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new AppValidationException(new Dictionary<string, string[]>
                {
                    { "Token", new[] { "Invalid or expired reset token" } }
                });
            }

            // Hash the new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Password reset successfully for user {UserId}", userId);
            return true;
        }
        catch (AppValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset password");
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                { "Token", new[] { "Failed to reset password" } }
            });
        }
    }
}
