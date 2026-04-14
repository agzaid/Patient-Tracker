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
            .OrderByDescending(s => s.SurgeryDate)
            .ToListAsync();
    }
}
