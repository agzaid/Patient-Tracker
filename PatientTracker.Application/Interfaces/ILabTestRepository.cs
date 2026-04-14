using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface ILabTestRepository : IGenericRepository<LabTest>
{
    Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId);
}
