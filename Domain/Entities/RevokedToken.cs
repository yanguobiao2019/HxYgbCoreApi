namespace Domain.Entities;

/// <summary>
/// 已撤销的 Access Token（JWT 黑名单）
/// </summary>
public class RevokedToken
{
    /// <summary>
    /// 主键
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// JWT Token ID (jti claim)
    /// </summary>
    public string TokenId { get; set; } = string.Empty;

    /// <summary>
    /// 用户 ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Token 原始值的哈希（用于快速查找）
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTime RevokedAt { get; set; }

    /// <summary>
    /// Token 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    public string? Reason { get; set; }
}

