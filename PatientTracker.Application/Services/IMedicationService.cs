using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IMedicationService
{
    Task<IEnumerable<MedicationDto>> GetMedicationsAsync(int userId);
    Task<MedicationDto?> GetMedicationAsync(int id, int userId);
    Task<MedicationDto> CreateMedicationAsync(int userId, CreateMedicationRequest request);
    Task<MedicationDto> UpdateMedicationAsync(int id, int userId, UpdateMedicationRequest request);
    Task<bool> DeleteMedicationAsync(int id, int userId);
}
