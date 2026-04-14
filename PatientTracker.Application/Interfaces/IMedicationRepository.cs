using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IMedicationRepository
{
    Task<IEnumerable<Medication>> GetByUserIdAsync(int userId);
    Task<Medication?> GetByIdAsync(int id);
    Task<Medication> CreateAsync(Medication medication);
    Task<Medication> UpdateAsync(Medication medication);
    Task<bool> DeleteAsync(int id);
}
