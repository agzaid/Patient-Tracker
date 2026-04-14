using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface ISurgeryRepository : IGenericRepository<Surgery>
{
    Task<IEnumerable<Surgery>> GetByUserIdAsync(int userId);
}
