using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IProfileRepository : IGenericRepository<Profile>
{
    Task<Profile?> GetByUserIdAsync(int userId);
}
