using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IDiagnosisService
{
    Task<IEnumerable<DiagnosisDto>> GetDiagnosesAsync(int userId);
    Task<DiagnosisDto?> GetDiagnosisAsync(int id, int userId);
    Task<DiagnosisDto> CreateDiagnosisAsync(int userId, CreateDiagnosisRequest request);
    Task<DiagnosisDto> UpdateDiagnosisAsync(int id, int userId, UpdateDiagnosisRequest request);
    Task<bool> DeleteDiagnosisAsync(int id, int userId);
}
