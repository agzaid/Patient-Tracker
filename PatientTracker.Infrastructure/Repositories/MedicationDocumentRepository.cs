using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class MedicationDocumentRepository : IMedicationDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public MedicationDocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MedicationDocument?> GetByIdAsync(int id)
    {
        return await _context.MedicationDocuments
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<MedicationDocument>> GetByUserIdAsync(int userId)
    {
        return await _context.MedicationDocuments
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<MedicationDocument>> GetByUserIdAsync(int userId, int page, int pageSize, string? search = null)
    {
        var query = _context.MedicationDocuments.Where(d => d.UserId == userId);
        
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
        var query = _context.MedicationDocuments.Where(d => d.UserId == userId);
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => 
                d.OriginalFileName.Contains(search) ||
                d.FileName.Contains(search));
        }
        
        return await query.CountAsync();
    }

    public void Add(MedicationDocument document)
    {
        _context.MedicationDocuments.Add(document);
    }

    public void Update(MedicationDocument document)
    {
        _context.MedicationDocuments.Update(document);
    }

    public void Delete(MedicationDocument document)
    {
        _context.MedicationDocuments.Remove(document);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.MedicationDocuments
            .AnyAsync(d => d.Id == id);
    }
}
