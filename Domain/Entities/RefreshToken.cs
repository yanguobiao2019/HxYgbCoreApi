namespace Domain.Entities;

/// <summary>
/// 刷新令牌实体
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    
    public string Token { get; set; } = string.Empty;
    
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public int? ClientId { get; set; }
    
    public Client? Client { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsRevoked { get; set; } = false;
    
    public DateTime? RevokedAt { get; set; }
    
    public string? Scopes { get; set; }
    
    /// <summary>
    /// JTI (JWT ID) - 用于关联访问令牌
    /// </summary>
    public string? JwtId { get; set; }
}

