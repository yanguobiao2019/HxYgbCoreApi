using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// 刷新令牌仓储接口
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(int userId);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
    Task RevokeAsync(string token);
    Task RevokeAllByUserIdAsync(int userId);
    Task DeleteExpiredAsync();
}

