using PatientTracker.Domain.Entities;
using PatientTracker.Domain.Enums;

namespace PatientTracker.Application.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(int id);
    Task<IEnumerable<Document>> GetByIdsAsync(IEnumerable<int> ids);
    Task<IEnumerable<Document>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Document>> GetByParentEntityAsync(ParentEntityType parentType, int parentId);
    Task<Document> AddAsync(Document document);
    void Update(Document document);
    void Delete(Document document);
    Task<bool> ExistsAsync(int id);
    Task<Document?> GetByFilePathAsync(string filePath);
}
