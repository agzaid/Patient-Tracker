using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface ISurgeryRepository
{
    Task<IEnumerable<Surgery>> GetByUserIdAsync(int userId);
    Task<Surgery?> GetByIdAsync(int id);
    Task<Surgery> CreateAsync(Surgery surgery);
    Task<Surgery> UpdateAsync(Surgery surgery);
    Task<bool> DeleteAsync(int id);
}
