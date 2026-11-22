using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 已撤销 Token 仓储实现
/// </summary>
public class RevokedTokenRepository : IRevokedTokenRepository
{
    private readonly AuthDbContext _context;

    public RevokedTokenRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RevokedToken revokedToken)
    {
        await _context.RevokedTokens.AddAsync(revokedToken);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsRevokedAsync(string tokenId)
    {
        return await _context.RevokedTokens
            .AnyAsync(rt => rt.TokenId == tokenId);
    }

    public async Task<bool> IsRevokedByHashAsync(string tokenHash)
    {
        return await _context.RevokedTokens
            .AnyAsync(rt => rt.TokenHash == tokenHash);
    }

    public async Task RevokeAllByUserIdAsync(int userId, string reason)
    {
        var now = DateTime.UtcNow;
        
        // 为该用户的所有未过期的可能 token 添加黑名单记录
        // 注意：这只是一个占位符记录，实际的 tokenId 需要从活动 token 中获取
        var revokedToken = new RevokedToken
        {
            TokenId = $"user_{userId}_{now.Ticks}",
            UserId = userId,
            TokenHash = string.Empty,
            RevokedAt = now,
            ExpiresAt = now.AddDays(1), // 假设 token 最长有效期为 1 天
            Reason = reason
        };

        await _context.RevokedTokens.AddAsync(revokedToken);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteExpiredAsync()
    {
        var expiredTokens = await _context.RevokedTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.RevokedTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync();
    }
}

