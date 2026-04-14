using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface ISharedLinkRepository : IGenericRepository<SharedLink>
{
    Task<IEnumerable<SharedLink>> GetByUserIdAsync(int userId);
    Task<SharedLink?> GetByTokenAsync(string token);
    Task<bool> IncrementAccessCountAsync(string token);
}
