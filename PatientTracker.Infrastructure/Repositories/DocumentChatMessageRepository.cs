using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class DocumentChatMessageRepository : IDocumentChatMessageRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentChatMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentChatMessage?> GetByIdAsync(int id)
    {
        return await _context.DocumentChatMessages
            .Include(m => m.Document)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<DocumentChatMessage>> GetByDocumentIdAsync(int documentId, int userId)
    {
        return await _context.DocumentChatMessages
            .Where(m => m.DocumentId == documentId && m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentChatMessage>> GetByDocumentIdAsync(int documentId, int userId, int page, int pageSize)
    {
        return await _context.DocumentChatMessages
            .Where(m => m.DocumentId == documentId && m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByDocumentIdAsync(int documentId, int userId)
    {
        return await _context.DocumentChatMessages
            .CountAsync(m => m.DocumentId == documentId && m.UserId == userId);
    }

    public void Add(DocumentChatMessage message)
    {
        _context.DocumentChatMessages.Add(message);
    }

    public void Update(DocumentChatMessage message)
    {
        _context.DocumentChatMessages.Update(message);
    }

    public void Delete(DocumentChatMessage message)
    {
        _context.DocumentChatMessages.Remove(message);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.DocumentChatMessages
            .AnyAsync(m => m.Id == id);
    }
}
