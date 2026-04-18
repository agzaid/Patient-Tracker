using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IDiagnosisService
{
    Task<IEnumerable<DiagnosisDto>> GetDiagnosesAsync(int userId);
    Task<PaginatedResponse<DiagnosisDto>> GetDiagnosesPaginatedAsync(int userId, int page = 1, int pageSize = 10, string? search = null);
    Task<DiagnosisDto?> GetDiagnosisAsync(int id, int userId);
    Task<DiagnosisDto> CreateDiagnosisAsync(int userId, CreateDiagnosisRequest request);
    Task<DiagnosisDto> UpdateDiagnosisAsync(int id, int userId, UpdateDiagnosisRequest request);
    Task<bool> DeleteDiagnosisAsync(int id, int userId);
}
