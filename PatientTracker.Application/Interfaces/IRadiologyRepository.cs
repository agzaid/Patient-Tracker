using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IRadiologyRepository
{
    Task<IEnumerable<RadiologyScan>> GetByUserIdAsync(int userId);
    Task<RadiologyScan?> GetByIdAsync(int id);
    Task<RadiologyScan> CreateAsync(RadiologyScan radiology);
    Task<RadiologyScan> UpdateAsync(RadiologyScan radiology);
    Task<bool> DeleteAsync(int id);
}
