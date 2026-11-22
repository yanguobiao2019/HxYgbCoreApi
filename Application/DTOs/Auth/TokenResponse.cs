namespace Application.DTOs.Auth;

/// <summary>
/// Token 响应
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    
    public string TokenType { get; set; } = "Bearer";
    
    public int ExpiresIn { get; set; }
    
    public string? RefreshToken { get; set; }
    
    public string? IdToken { get; set; }
    
    public string? Scope { get; set; }
}

