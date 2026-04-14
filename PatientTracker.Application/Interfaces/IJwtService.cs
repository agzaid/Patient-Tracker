using PatientTracker.Application.DTOs;
using System.Security.Claims;

namespace PatientTracker.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(UserDto user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
