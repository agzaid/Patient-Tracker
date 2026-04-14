using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IRadiologyRepository : IGenericRepository<RadiologyScan>
{
    Task<IEnumerable<RadiologyScan>> GetByUserIdAsync(int userId);
}
