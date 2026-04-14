using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class ProfileRepository : GenericRepository<Profile>, IProfileRepository
{
    public ProfileRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Profile?> GetByUserIdAsync(int userId)
    {
        return await _context.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }
}
