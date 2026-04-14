using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class SharedLinkRepository : ISharedLinkRepository
{
    private readonly ApplicationDbContext _context;

    public SharedLinkRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SharedLink>> GetByUserIdAsync(int userId)
    {
        return await _context.SharedLinks
            .Where(sl => sl.UserId == userId)
            .OrderByDescending(sl => sl.CreatedAt)
            .ToListAsync();
    }

    public async Task<SharedLink?> GetByTokenAsync(string token)
    {
        return await _context.SharedLinks
            .Include(sl => sl.User)
            .ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(sl => sl.Token == token);
    }

    public async Task<SharedLink?> GetByIdAsync(int id)
    {
        return await _context.SharedLinks.FindAsync(id);
    }

    public async Task<SharedLink> CreateAsync(SharedLink link)
    {
        _context.SharedLinks.Add(link);
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task<SharedLink> UpdateAsync(SharedLink link)
    {
        _context.SharedLinks.Update(link);
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var link = await _context.SharedLinks.FindAsync(id);
        if (link == null) return false;

        _context.SharedLinks.Remove(link);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IncrementAccessCountAsync(string token)
    {
        var link = await _context.SharedLinks.FirstOrDefaultAsync(sl => sl.Token == token);
        if (link == null) return false;

        link.AccessCount++;
        await _context.SaveChangesAsync();
        return true;
    }
}
