using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class LabTestDocumentRepository : ILabTestDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public LabTestDocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LabTestDocument?> GetByIdAsync(int id)
    {
        return await _context.LabTestDocuments
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<LabTestDocument>> GetByUserIdAsync(int userId)
    {
        return await _context.LabTestDocuments
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LabTestDocument>> GetByUserIdAsync(int userId, int page, int pageSize, string? search = null)
    {
        var query = _context.LabTestDocuments.Where(d => d.UserId == userId);
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => 
                d.OriginalFileName.Contains(search) ||
                d.FileName.Contains(search));
        }
        
        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId, string? search = null)
    {
        var query = _context.LabTestDocuments.Where(d => d.UserId == userId);
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => 
                d.OriginalFileName.Contains(search) ||
                d.FileName.Contains(search));
        }
        
        return await query.CountAsync();
    }

    public async Task<LabTestDocument?> GetByIdWithTestsAsync(int id)
    {
        return await _context.LabTestDocuments
            .Include(d => d.LabTests)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public void Add(LabTestDocument document)
    {
        _context.LabTestDocuments.Add(document);
    }

    public void Update(LabTestDocument document)
    {
        _context.LabTestDocuments.Update(document);
    }

    public void Delete(LabTestDocument document)
    {
        _context.LabTestDocuments.Remove(document);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.LabTestDocuments
            .AnyAsync(d => d.Id == id);
    }
}
