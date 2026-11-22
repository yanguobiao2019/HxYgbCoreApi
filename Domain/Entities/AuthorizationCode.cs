namespace Domain.Entities;

/// <summary>
/// 授权码实体（用于 OAuth 2.0 授权码流程）
/// </summary>
public class AuthorizationCode
{
    public int Id { get; set; }
    
    public string Code { get; set; } = string.Empty;
    
    public int ClientId { get; set; }
    
    public Client Client { get; set; } = null!;
    
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public string RedirectUri { get; set; } = string.Empty;
    
    public string? Scopes { get; set; }
    
    /// <summary>
    /// PKCE Code Challenge
    /// </summary>
    public string? CodeChallenge { get; set; }
    
    /// <summary>
    /// PKCE Code Challenge Method (plain 或 S256)
    /// </summary>
    public string? CodeChallengeMethod { get; set; }
    
    /// <summary>
    /// Nonce 用于防止重放攻击
    /// </summary>
    public string? Nonce { get; set; }
    
    /// <summary>
    /// State 参数，用于防止CSRF攻击
    /// </summary>
    public string? State { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    public DateTime? UsedAt { get; set; }
}

