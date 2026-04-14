using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IDiagnosisRepository
{
    Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId);
    Task<Diagnosis?> GetByIdAsync(int id);
    Task<Diagnosis> CreateAsync(Diagnosis diagnosis);
    Task<Diagnosis> UpdateAsync(Diagnosis diagnosis);
    Task<bool> DeleteAsync(int id);
}
