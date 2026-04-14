using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IMedicationRepository : IGenericRepository<Medication>
{
    Task<IEnumerable<Medication>> GetByUserIdAsync(int userId);
}
