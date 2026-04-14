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
            .ToListAsync();
    }
}
