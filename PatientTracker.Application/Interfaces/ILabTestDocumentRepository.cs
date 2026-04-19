using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface ILabTestDocumentRepository
{
    Task<LabTestDocument?> GetByIdAsync(int id);
    Task<IEnumerable<LabTestDocument>> GetByUserIdAsync(int userId);
    Task<IEnumerable<LabTestDocument>> GetByUserIdAsync(int userId, int page, int pageSize, string? search = null);
    Task<int> CountByUserIdAsync(int userId, string? search = null);
    Task<LabTestDocument?> GetByIdWithTestsAsync(int id);
    void Add(LabTestDocument document);
    void Update(LabTestDocument document);
    void Delete(LabTestDocument document);
    Task<bool> ExistsAsync(int id);
}
