using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface ISharedLinkRepository
{
    Task<IEnumerable<SharedLink>> GetByUserIdAsync(int userId);
    Task<SharedLink?> GetByTokenAsync(string token);
    Task<SharedLink?> GetByIdAsync(int id);
    Task<SharedLink> CreateAsync(SharedLink link);
    Task<SharedLink> UpdateAsync(SharedLink link);
    Task<bool> DeleteAsync(int id);
    Task<bool> IncrementAccessCountAsync(string token);
}
