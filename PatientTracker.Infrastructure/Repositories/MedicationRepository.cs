using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class MedicationRepository : GenericRepository<Medication>, IMedicationRepository
{
    public MedicationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Medication>> GetByUserIdAsync(int userId)
    {
        return await _context.Medications
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Medication>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        return await _context.Medications
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId)
    {
        return await _context.Medications
            .CountAsync(m => m.UserId == userId);
    }

    public async Task<IEnumerable<Medication>> GetByUserIdAsync(int userId, int page, int pageSize, string? search)
    {
        var query = _context.Medications.Where(m => m.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => 
                m.Name.Contains(search) ||
                (m.Dosage != null && m.Dosage.Contains(search)) ||
                (m.Frequency != null && m.Frequency.Contains(search)) ||
                (m.Notes != null && m.Notes.Contains(search)));
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId, string? search)
    {
        var query = _context.Medications.Where(m => m.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => 
                m.Name.Contains(search) ||
                (m.Dosage != null && m.Dosage.Contains(search)) ||
                (m.Frequency != null && m.Frequency.Contains(search)) ||
                (m.Notes != null && m.Notes.Contains(search)));
        }

        return await query.CountAsync();
    }
}
