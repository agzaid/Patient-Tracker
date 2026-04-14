using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task<RefreshToken?> UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
        return Task.FromResult<RefreshToken?>(refreshToken);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken == null) return false;

        refreshToken.IsRevoked = true;
        return true;
    }

    public async Task<bool> RevokeAllUserRefreshTokensAsync(int userId)
    {
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in refreshTokens)
        {
            token.IsRevoked = true;
        }

        return true;
    }
}
