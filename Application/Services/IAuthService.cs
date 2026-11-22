using Application.DTOs.Auth;
using Application.DTOs.User;

namespace Application.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 用户登录
    /// </summary>
    Task<TokenResponse> LoginAsync(LoginRequest request);
    
    /// <summary>
    /// 用户注册
    /// </summary>
    Task<UserDto> RegisterAsync(RegisterRequest request);
    
    /// <summary>
    /// 刷新令牌
    /// </summary>
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    
    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    Task RevokeTokenAsync(string refreshToken);
    
    /// <summary>
    /// 撤销用户的所有令牌
    /// </summary>
    Task RevokeAllUserTokensAsync(int userId);
}

