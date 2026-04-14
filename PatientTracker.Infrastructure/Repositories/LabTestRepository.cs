using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class LabTestRepository : GenericRepository<LabTest>, ILabTestRepository
{
    public LabTestRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId)
    {
        return await _context.LabTests
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.TestDate)
            .ToListAsync();
    }
}
