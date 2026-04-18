using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface ILabTestService
{
    Task<IEnumerable<LabTestDto>> GetLabTestsAsync(int userId);
    Task<PaginatedResponse<LabTestDto>> GetLabTestsPaginatedAsync(int userId, int page = 1, int pageSize = 10, string? search = null);
    Task<LabTestDto?> GetLabTestAsync(int id, int userId);
    Task<LabTestDto> CreateLabTestAsync(int userId, CreateLabTestRequest request);
    Task<LabTestDto> UpdateLabTestAsync(int id, int userId, UpdateLabTestRequest request);
    Task<bool> DeleteLabTestAsync(int id, int userId);
}
