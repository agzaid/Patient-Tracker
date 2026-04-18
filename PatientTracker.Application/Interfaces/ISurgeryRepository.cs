using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface ISurgeryRepository : IGenericRepository<Surgery>
{
    Task<IEnumerable<Surgery>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Surgery>> GetByUserIdAsync(int userId, int page, int pageSize);
    Task<IEnumerable<Surgery>> GetByUserIdAsync(int userId, int page, int pageSize, string? search);
    Task<int> CountByUserIdAsync(int userId);
    Task<int> CountByUserIdAsync(int userId, string? search);
}
