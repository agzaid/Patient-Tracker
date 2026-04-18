using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IRadiologyRepository : IGenericRepository<RadiologyScan>
{
    Task<IEnumerable<RadiologyScan>> GetByUserIdAsync(int userId);
    Task<IEnumerable<RadiologyScan>> GetByUserIdAsync(int userId, int page, int pageSize);
    Task<IEnumerable<RadiologyScan>> GetByUserIdAsync(int userId, int page, int pageSize, string? search);
    Task<int> CountByUserIdAsync(int userId);
    Task<int> CountByUserIdAsync(int userId, string? search);
}
