using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken);
    Task<RefreshToken?> UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task<bool> RevokeRefreshTokenAsync(string token);
    Task<bool> RevokeAllUserRefreshTokensAsync(int userId);
}
