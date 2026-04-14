using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface ILabTestRepository
{
    Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId);
    Task<LabTest?> GetByIdAsync(int id);
    Task<LabTest> CreateAsync(LabTest labTest);
    Task<LabTest> UpdateAsync(LabTest labTest);
    Task<bool> DeleteAsync(int id);
}
