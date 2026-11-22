using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Client;

/// <summary>
/// 创建客户端请求
/// </summary>
public class CreateClientRequest
{
    [Required(ErrorMessage = "客户端ID不能为空")]
    public string ClientId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "客户端名称不能为空")]
    public string ClientName { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "至少需要一个重定向URI")]
    public string[] RedirectUris { get; set; } = Array.Empty<string>();
    
    public string[] PostLogoutRedirectUris { get; set; } = Array.Empty<string>();
    
    public string[] AllowedGrantTypes { get; set; } = new[] { "authorization_code" };
    
    public string[] AllowedScopes { get; set; } = new[] { "openid", "profile", "email" };
    
    public int AccessTokenLifetime { get; set; } = 3600;
    
    public int RefreshTokenLifetime { get; set; } = 2592000;
    
    public bool RequireConsent { get; set; } = true;
    
    public bool RequirePkce { get; set; } = true;
}

