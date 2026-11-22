namespace Domain.Entities;

/// <summary>
/// OAuth 2.0 客户端实体
/// </summary>
public class Client
{
    public int Id { get; set; }
    
    public string ClientId { get; set; } = string.Empty;
    
    public string ClientSecret { get; set; } = string.Empty;
    
    public string ClientName { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    /// <summary>
    /// 允许的重定向 URI（逗号分隔）
    /// </summary>
    public string RedirectUris { get; set; } = string.Empty;
    
    /// <summary>
    /// 登出后重定向 URI（逗号分隔）
    /// </summary>
    public string? PostLogoutRedirectUris { get; set; }
    
    /// <summary>
    /// 允许的授权类型（逗号分隔）
    /// authorization_code, client_credentials, password, refresh_token, implicit
    /// </summary>
    public string AllowedGrantTypes { get; set; } = "authorization_code";
    
    /// <summary>
    /// 允许的作用域（逗号分隔）
    /// </summary>
    public string AllowedScopes { get; set; } = "openid,profile,email";
    
    /// <summary>
    /// 访问令牌生命周期（秒）
    /// </summary>
    public int AccessTokenLifetime { get; set; } = 3600;
    
    /// <summary>
    /// 刷新令牌生命周期（秒）
    /// </summary>
    public int RefreshTokenLifetime { get; set; } = 2592000; // 30天
    
    /// <summary>
    /// 授权码生命周期（秒）
    /// </summary>
    public int AuthorizationCodeLifetime { get; set; } = 300; // 5分钟
    
    /// <summary>
    /// 是否需要同意确认
    /// </summary>
    public bool RequireConsent { get; set; } = true;
    
    /// <summary>
    /// 是否允许记住同意
    /// </summary>
    public bool AllowRememberConsent { get; set; } = true;
    
    /// <summary>
    /// 是否启用PKCE
    /// </summary>
    public bool RequirePkce { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public ICollection<AuthorizationCode> AuthorizationCodes { get; set; } = new List<AuthorizationCode>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

