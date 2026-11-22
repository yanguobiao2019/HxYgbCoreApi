using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// 已撤销 Token 仓储接口
/// </summary>
public interface IRevokedTokenRepository
{
    /// <summary>
    /// 添加 Token 到黑名单
    /// </summary>
    Task AddAsync(RevokedToken revokedToken);

    /// <summary>
    /// 检查 Token 是否已撤销
    /// </summary>
    Task<bool> IsRevokedAsync(string tokenId);

    /// <summary>
    /// 撤销用户的所有 Token
    /// </summary>
    Task RevokeAllByUserIdAsync(int userId, string reason);

    /// <summary>
    /// 删除过期的黑名单记录
    /// </summary>
    Task DeleteExpiredAsync();

    /// <summary>
    /// 根据 Token 哈希检查是否已撤销
    /// </summary>
    Task<bool> IsRevokedByHashAsync(string tokenHash);
}

