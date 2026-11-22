using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.OAuth;

/// <summary>
/// OAuth 2.0 Token 请求
/// </summary>
public class TokenRequest
{
    [Required(ErrorMessage = "授权类型不能为空")]
    public string GrantType { get; set; } = string.Empty; // authorization_code, client_credentials, password, refresh_token
    
    public string? Code { get; set; }
    
    public string? RedirectUri { get; set; }
    
    [Required(ErrorMessage = "客户端ID不能为空")]
    public string ClientId { get; set; } = string.Empty;
    
    public string? ClientSecret { get; set; }
    
    // PKCE 参数
    public string? CodeVerifier { get; set; }
    
    // Password Grant
    public string? Username { get; set; }
    
    public string? Password { get; set; }
    
    // Refresh Token Grant
    public string? RefreshToken { get; set; }
    
    public string? Scope { get; set; }
}

