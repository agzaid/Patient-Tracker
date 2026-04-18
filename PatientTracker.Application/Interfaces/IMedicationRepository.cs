using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IMedicationRepository : IGenericRepository<Medication>
{
    Task<IEnumerable<Medication>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Medication>> GetByUserIdAsync(int userId, int page, int pageSize);
    Task<IEnumerable<Medication>> GetByUserIdAsync(int userId, int page, int pageSize, string? search);
    Task<int> CountByUserIdAsync(int userId);
    Task<int> CountByUserIdAsync(int userId, string? search);
}
