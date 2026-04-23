using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Domain.Enums;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        return await _context.Documents
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Document?> GetByIdAsync(int? id)
    {
        if (!id.HasValue)
            return null;
            
        return await GetByIdAsync(id.Value);
    }

    public async Task<IEnumerable<Document>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return await _context.Documents
            .Where(d => ids.Contains(d.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetByUserIdAsync(int userId)
    {
        return await _context.Documents
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetByParentEntityAsync(ParentEntityType parentType, int parentId)
    {
        return await _context.Documents
            .Where(d => d.ParentEntityType == parentType && d.ParentEntityId == parentId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Document> AddAsync(Document document)
    {
        await _context.Documents.AddAsync(document);
        return document;
    }

    public void Update(Document document)
    {
        _context.Documents.Update(document);
    }

    public void Delete(Document document)
    {
        _context.Documents.Remove(document);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Documents.AnyAsync(d => d.Id == id);
    }

    public async Task<Document?> GetByFilePathAsync(string filePath)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(d => d.FilePath == filePath);
    }
}
