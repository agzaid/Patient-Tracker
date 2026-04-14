using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface ISurgeryService
{
    Task<IEnumerable<SurgeryDto>> GetSurgeriesAsync(int userId);
    Task<SurgeryDto?> GetSurgeryAsync(int id, int userId);
    Task<SurgeryDto> CreateSurgeryAsync(int userId, CreateSurgeryRequest request);
    Task<SurgeryDto> UpdateSurgeryAsync(int id, int userId, UpdateSurgeryRequest request);
    Task<bool> DeleteSurgeryAsync(int id, int userId);
}
