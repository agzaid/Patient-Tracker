using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IRadiologyService
{
    Task<IEnumerable<RadiologyScanDto>> GetRadiologyScansAsync(int userId);
    Task<RadiologyScanDto?> GetRadiologyScanAsync(int id, int userId);
    Task<RadiologyScanDto> CreateRadiologyScanAsync(int userId, CreateRadiologyScanRequest request);
    Task<RadiologyScanDto> UpdateRadiologyScanAsync(int id, int userId, UpdateRadiologyScanRequest request);
    Task<bool> DeleteRadiologyScanAsync(int id, int userId);
}
