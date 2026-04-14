using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class SurgeryRepository : ISurgeryRepository
{
    private readonly ApplicationDbContext _context;

    public SurgeryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Surgery>> GetByUserIdAsync(int userId)
    {
        return await _context.Surgeries
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SurgeryDate)
            .ToListAsync();
    }

    public async Task<Surgery?> GetByIdAsync(int id)
    {
        return await _context.Surgeries.FindAsync(id);
    }

    public async Task<Surgery> CreateAsync(Surgery surgery)
    {
        _context.Surgeries.Add(surgery);
        await _context.SaveChangesAsync();
        return surgery;
    }

    public async Task<Surgery> UpdateAsync(Surgery surgery)
    {
        _context.Surgeries.Update(surgery);
        await _context.SaveChangesAsync();
        return surgery;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var surgery = await _context.Surgeries.FindAsync(id);
        if (surgery == null) return false;

        _context.Surgeries.Remove(surgery);
        await _context.SaveChangesAsync();
        return true;
    }
}
