using PatientTracker.Application.DTOs;
using System.Threading.Tasks;

namespace PatientTracker.Application.Services;

public interface IPasswordResetService
{
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task<bool> ValidateResetTokenAsync(string token);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}
