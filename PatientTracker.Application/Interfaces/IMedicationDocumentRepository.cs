using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IMedicationDocumentRepository
{
    Task<MedicationDocument?> GetByIdAsync(int id);
    Task<IEnumerable<MedicationDocument>> GetByUserIdAsync(int userId);
    Task<IEnumerable<MedicationDocument>> GetByUserIdAsync(int userId, int page, int pageSize, string? search = null);
    Task<int> GetCountByUserIdAsync(int userId, string? search = null);
    void Add(MedicationDocument document);
    void Update(MedicationDocument document);
    void Delete(MedicationDocument document);
    Task<bool> ExistsAsync(int id);
}
