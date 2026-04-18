using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface ILabTestRepository : IGenericRepository<LabTest>
{
    Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId);
    Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId, int page, int pageSize);
    Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId, int page, int pageSize, string? search);
    Task<int> CountByUserIdAsync(int userId);
    Task<int> CountByUserIdAsync(int userId, string? search);
}
