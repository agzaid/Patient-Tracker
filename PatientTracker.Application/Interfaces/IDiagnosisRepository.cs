using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IDiagnosisRepository : IGenericRepository<Diagnosis>
{
    Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId, int page, int pageSize);
    Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId, int page, int pageSize, string? search);
    Task<int> CountByUserIdAsync(int userId);
    Task<int> CountByUserIdAsync(int userId, string? search);
}
