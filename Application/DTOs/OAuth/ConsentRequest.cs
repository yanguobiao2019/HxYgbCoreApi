namespace Application.DTOs.OAuth;

/// <summary>
/// 用户同意请求
/// </summary>
public class ConsentRequest
{
    public string ClientId { get; set; } = string.Empty;
    
    public string[] GrantedScopes { get; set; } = Array.Empty<string>();
    
    public bool RememberConsent { get; set; }
    
    public string? ReturnUrl { get; set; }
}

