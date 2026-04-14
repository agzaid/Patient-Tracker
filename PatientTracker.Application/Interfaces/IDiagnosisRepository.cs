using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IDiagnosisRepository : IGenericRepository<Diagnosis>
{
    Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId);
}
