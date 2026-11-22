namespace Domain.Entities;

/// <summary>
/// 用户同意记录（用于记住用户的授权同意）
/// </summary>
public class UserConsent
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public int ClientId { get; set; }
    
    public Client Client { get; set; } = null!;
    
    /// <summary>
    /// 已同意的作用域（逗号分隔）
    /// </summary>
    public string Scopes { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
}

