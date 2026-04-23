using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class SurgeryRepository : GenericRepository<Surgery>, ISurgeryRepository
{
    public SurgeryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Surgery>> GetByUserIdAsync(int userId)
    {
        return await _context.Surgeries
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Surgery>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        return await _context.Surgeries
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Surgery>> GetByUserIdAsync(int userId, int page, int pageSize, string? search)
    {
        var query = _context.Surgeries.Where(s => s.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => 
                s.SurgeryName.Contains(search) ||
                (s.HospitalName != null && s.HospitalName.Contains(search)) ||
                (s.SurgeonName != null && s.SurgeonName.Contains(search)) ||
                (s.Description != null && s.Description.Contains(search)) ||
                (s.Notes != null && s.Notes.Contains(search)));
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId)
    {
        return await _context.Surgeries
            .CountAsync(s => s.UserId == userId);
    }

    public async Task<int> CountByUserIdAsync(int userId, string? search)
    {
        var query = _context.Surgeries.Where(s => s.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => 
                s.SurgeryName.Contains(search) ||
                (s.HospitalName != null && s.HospitalName.Contains(search)) ||
                (s.SurgeonName != null && s.SurgeonName.Contains(search)) ||
                (s.Description != null && s.Description.Contains(search)) ||
                (s.Notes != null && s.Notes.Contains(search)));
        }

        return await query.CountAsync();
    }
}
