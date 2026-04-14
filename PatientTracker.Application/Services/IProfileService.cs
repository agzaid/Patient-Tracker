using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IProfileService
{
    Task<ProfileDto?> GetProfileAsync(int userId);
    Task<ProfileDto> CreateProfileAsync(int userId, CreateProfileRequest request);
    Task<ProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<bool> DeleteProfileAsync(int userId);
}
