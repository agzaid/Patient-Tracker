using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class RadiologyRepository : IRadiologyRepository
{
    private readonly ApplicationDbContext _context;

    public RadiologyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RadiologyScan>> GetByUserIdAsync(int userId)
    {
        return await _context.RadiologyScans
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task<RadiologyScan?> GetByIdAsync(int id)
    {
        return await _context.RadiologyScans
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<RadiologyScan> CreateAsync(RadiologyScan radiology)
    {
        _context.RadiologyScans.Add(radiology);
        await _context.SaveChangesAsync();
        return radiology;
    }

    public async Task<RadiologyScan> UpdateAsync(RadiologyScan radiology)
    {
        _context.RadiologyScans.Update(radiology);
        await _context.SaveChangesAsync();
        return radiology;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var radiology = await _context.RadiologyScans
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (radiology == null) return false;

        _context.RadiologyScans.Remove(radiology);
        await _context.SaveChangesAsync();
        return true;
    }
}
