using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class DiagnosisDocumentRepository : IDiagnosisDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DiagnosisDocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DiagnosisDocument?> GetByIdAsync(int id)
    {
        return await _context.DiagnosisDocuments
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<DiagnosisDocument>> GetByUserIdAsync(int userId)
    {
        return await _context.DiagnosisDocuments
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DiagnosisDocument>> GetByUserIdAsync(int userId, int page, int pageSize, string? search = null)
    {
        var query = _context.DiagnosisDocuments.Where(d => d.UserId == userId);
        
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

    public async Task<int> GetCountByUserIdAsync(int userId, string? search = null)
    {
        var query = _context.DiagnosisDocuments.Where(d => d.UserId == userId);
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => 
                d.OriginalFileName.Contains(search) ||
                d.FileName.Contains(search));
        }
        
        return await query.CountAsync();
    }

    public void Add(DiagnosisDocument document)
    {
        _context.DiagnosisDocuments.Add(document);
    }

    public void Update(DiagnosisDocument document)
    {
        _context.DiagnosisDocuments.Update(document);
    }

    public void Delete(DiagnosisDocument document)
    {
        _context.DiagnosisDocuments.Remove(document);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.DiagnosisDocuments
            .AnyAsync(d => d.Id == id);
    }
}
