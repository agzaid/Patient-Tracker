using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IDiagnosisDocumentRepository
{
    Task<DiagnosisDocument?> GetByIdAsync(int id);
    Task<IEnumerable<DiagnosisDocument>> GetByUserIdAsync(int userId);
    Task<IEnumerable<DiagnosisDocument>> GetByUserIdAsync(int userId, int page, int pageSize, string? search = null);
    Task<int> GetCountByUserIdAsync(int userId, string? search = null);
    void Add(DiagnosisDocument document);
    void Update(DiagnosisDocument document);
    void Delete(DiagnosisDocument document);
    Task<bool> ExistsAsync(int id);
}
