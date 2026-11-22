using System.Security.Claims;
using Application.DTOs.Auth;

namespace Application.Services;

/// <summary>
/// Token 服务接口
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// 生成访问令牌
    /// </summary>
    string GenerateAccessToken(IEnumerable<Claim> claims, int expirationMinutes = 60);
    
    /// <summary>
    /// 生成刷新令牌
    /// </summary>
    string GenerateRefreshToken();
    
    /// <summary>
    /// 生成 ID Token（OpenID Connect）
    /// </summary>
    string GenerateIdToken(IEnumerable<Claim> claims, string clientId, string? nonce = null);
    
    /// <summary>
    /// 验证访问令牌
    /// </summary>
    ClaimsPrincipal? ValidateAccessToken(string token);
    
    /// <summary>
    /// 从 Token 中获取用户 ID
    /// </summary>
    int? GetUserIdFromToken(string token);
}

