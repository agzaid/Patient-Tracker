using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface ILabTestService
{
    Task<IEnumerable<LabTestDto>> GetLabTestsAsync(int userId);
    Task<LabTestDto?> GetLabTestAsync(int id, int userId);
    Task<LabTestDto> CreateLabTestAsync(int userId, CreateLabTestRequest request);
    Task<LabTestDto> UpdateLabTestAsync(int id, int userId, UpdateLabTestRequest request);
    Task<bool> DeleteLabTestAsync(int id, int userId);
}
