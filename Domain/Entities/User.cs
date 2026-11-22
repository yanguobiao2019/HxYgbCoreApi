namespace Domain.Entities;

/// <summary>
/// 用户实体
/// </summary>
public class User
{
    public int Id { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    
    public string? FullName { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// 用户角色（逗号分隔）
    /// </summary>
    public string Roles { get; set; } = "User";
    
    /// <summary>
    /// 用户声明（Claims），JSON格式
    /// </summary>
    public string? Claims { get; set; }
    
    // 导航属性
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<AuthorizationCode> AuthorizationCodes { get; set; } = new List<AuthorizationCode>();
}

