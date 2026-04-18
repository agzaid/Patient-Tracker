using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IMedicationService
{
    Task<IEnumerable<MedicationDto>> GetMedicationsAsync(int userId);
    Task<PaginatedResponse<MedicationDto>> GetMedicationsPaginatedAsync(int userId, int page = 1, int pageSize = 10, string? search = null);
    Task<MedicationDto?> GetMedicationAsync(int id, int userId);
    Task<MedicationDto> CreateMedicationAsync(int userId, CreateMedicationRequest request);
    Task<MedicationDto> UpdateMedicationAsync(int id, int userId, UpdateMedicationRequest request);
    Task<bool> DeleteMedicationAsync(int id, int userId);
}
