using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.OAuth;

/// <summary>
/// OAuth 2.0 授权请求
/// </summary>
public class AuthorizeRequest
{
    [Required(ErrorMessage = "客户端ID不能为空")]
    public string ClientId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "响应类型不能为空")]
    public string ResponseType { get; set; } = "code"; // code, token, id_token
    
    [Required(ErrorMessage = "重定向URI不能为空")]
    public string RedirectUri { get; set; } = string.Empty;
    
    public string? Scope { get; set; } = "openid profile email";
    
    public string? State { get; set; }
    
    public string? Nonce { get; set; }
    
    // PKCE 参数
    public string? CodeChallenge { get; set; }
    
    public string? CodeChallengeMethod { get; set; } // plain, S256
    
    public string? ResponseMode { get; set; } // query, fragment, form_post
}

