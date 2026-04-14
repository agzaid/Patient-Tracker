using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken);
    Task<RefreshToken?> UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task<bool> RevokeRefreshTokenAsync(string token);
    Task<bool> RevokeAllUserRefreshTokensAsync(int userId);
}
