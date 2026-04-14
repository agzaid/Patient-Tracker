using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface ISharedLinkService
{
    Task<IEnumerable<SharedLinkDto>> GetSharedLinksAsync(int userId);
    Task<SharedLinkDto> CreateSharedLinkAsync(int userId, CreateSharedLinkRequest request);
    Task<bool> DeleteSharedLinkAsync(int id, int userId);
    Task<bool> ToggleSharedLinkAsync(int id, int userId);
    Task<SharedProfileResponse?> GetSharedProfileAsync(string token);
}
