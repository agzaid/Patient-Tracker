using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IDocumentChatMessageRepository
{
    Task<DocumentChatMessage?> GetByIdAsync(int id);
    Task<IEnumerable<DocumentChatMessage>> GetByDocumentIdAsync(int documentId, int userId);
    Task<IEnumerable<DocumentChatMessage>> GetByDocumentIdAsync(int documentId, int userId, int page, int pageSize);
    Task<int> CountByDocumentIdAsync(int documentId, int userId);
    void Add(DocumentChatMessage message);
    void Update(DocumentChatMessage message);
    void Delete(DocumentChatMessage message);
    Task<bool> ExistsAsync(int id);
}
