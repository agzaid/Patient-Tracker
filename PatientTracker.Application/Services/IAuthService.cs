using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(string refreshToken);
    Task<string> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}
