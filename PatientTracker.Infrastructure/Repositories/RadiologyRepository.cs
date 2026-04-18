using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class RadiologyRepository : GenericRepository<RadiologyScan>, IRadiologyRepository
{
    public RadiologyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<RadiologyScan>> GetByUserIdAsync(int userId)
    {
        return await _context.RadiologyScans
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ScanDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<RadiologyScan>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        return await _context.RadiologyScans
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ScanDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<RadiologyScan>> GetByUserIdAsync(int userId, int page, int pageSize, string? search)
    {
        var query = _context.RadiologyScans.Where(r => r.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => 
                r.ScanType.Contains(search) ||
                (r.BodyPart != null && r.BodyPart.Contains(search)) ||
                (r.Description != null && r.Description.Contains(search)) ||
                (r.DoctorNotes != null && r.DoctorNotes.Contains(search)) ||
                (r.HospitalName != null && r.HospitalName.Contains(search)) ||
                (r.DoctorName != null && r.DoctorName.Contains(search)));
        }

        return await query
            .OrderByDescending(r => r.ScanDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId)
    {
        return await _context.RadiologyScans
            .CountAsync(r => r.UserId == userId);
    }

    public async Task<int> CountByUserIdAsync(int userId, string? search)
    {
        var query = _context.RadiologyScans.Where(r => r.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => 
                r.ScanType.Contains(search) ||
                (r.BodyPart != null && r.BodyPart.Contains(search)) ||
                (r.Description != null && r.Description.Contains(search)) ||
                (r.DoctorNotes != null && r.DoctorNotes.Contains(search)) ||
                (r.HospitalName != null && r.HospitalName.Contains(search)) ||
                (r.DoctorName != null && r.DoctorName.Contains(search)));
        }

        return await query.CountAsync();
    }
}
