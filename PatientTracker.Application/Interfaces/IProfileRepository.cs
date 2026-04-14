using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IProfileRepository
{
    Task<Profile?> GetByUserIdAsync(int userId);
    Task<Profile> CreateAsync(Profile profile);
    Task<Profile> UpdateAsync(Profile profile);
    Task<bool> DeleteAsync(int id);
}
