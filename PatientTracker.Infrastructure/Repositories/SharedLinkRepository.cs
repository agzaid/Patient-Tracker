using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class SharedLinkRepository : GenericRepository<SharedLink>, ISharedLinkRepository
{
    public SharedLinkRepository(ApplicationDbContext context) : base(context)
    {
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

    public async Task<bool> IncrementAccessCountAsync(string token)
    {
        var link = await _context.SharedLinks.FirstOrDefaultAsync(sl => sl.Token == token);
        if (link == null) return false;

        link.AccessCount++;
        return true;
    }
}
