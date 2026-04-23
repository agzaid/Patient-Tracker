using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class DiagnosisRepository : GenericRepository<Diagnosis>, IDiagnosisRepository
{
    public DiagnosisRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId)
    {
        return await _context.Diagnoses
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        return await _context.Diagnoses
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId, int page, int pageSize, string? search)
    {
        var query = _context.Diagnoses.Where(d => d.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => 
                d.DiagnosisName.Contains(search) ||
                (d.DoctorName != null && d.DoctorName.Contains(search)) ||
                (d.Severity != null && d.Severity.Contains(search)) ||
                (d.Status != null && d.Status.Contains(search)) ||
                (d.Notes != null && d.Notes.Contains(search)));
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId)
    {
        return await _context.Diagnoses
            .CountAsync(d => d.UserId == userId);
    }

    public async Task<int> CountByUserIdAsync(int userId, string? search)
    {
        var query = _context.Diagnoses.Where(d => d.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => 
                d.DiagnosisName.Contains(search) ||
                (d.DoctorName != null && d.DoctorName.Contains(search)) ||
                (d.Severity != null && d.Severity.Contains(search)) ||
                (d.Status != null && d.Status.Contains(search)) ||
                (d.Notes != null && d.Notes.Contains(search)));
        }

        return await query.CountAsync();
    }
}
