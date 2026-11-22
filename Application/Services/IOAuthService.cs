using Application.DTOs.Auth;
using Application.DTOs.OAuth;

namespace Application.Services;

/// <summary>
/// OAuth 2.0 服务接口
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// 处理授权请求
    /// </summary>
    Task<string> AuthorizeAsync(AuthorizeRequest request, int userId);
    
    /// <summary>
    /// 验证授权请求
    /// </summary>
    Task<bool> ValidateAuthorizeRequestAsync(AuthorizeRequest request);
    
    /// <summary>
    /// 处理 Token 请求
    /// </summary>
    Task<TokenResponse> TokenAsync(TokenRequest request);
    
    /// <summary>
    /// 处理用户同意
    /// </summary>
    Task SaveConsentAsync(ConsentRequest request, int userId);
    
    /// <summary>
    /// 获取用户已同意的作用域
    /// </summary>
    Task<string[]?> GetConsentedScopesAsync(int userId, string clientId);
}

