using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class ProfileRepository : IProfileRepository
{
    private readonly ApplicationDbContext _context;

    public ProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Profile?> GetByUserIdAsync(int userId)
    {
        return await _context.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Profile> CreateAsync(Profile profile)
    {
        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    public async Task<Profile> UpdateAsync(Profile profile)
    {
        _context.Profiles.Update(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var profile = await _context.Profiles.FindAsync(id);
        if (profile == null) return false;

        _context.Profiles.Remove(profile);
        await _context.SaveChangesAsync();
        return true;
    }
}
