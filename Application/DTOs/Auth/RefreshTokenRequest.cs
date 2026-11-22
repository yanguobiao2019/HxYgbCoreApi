using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

/// <summary>
/// 刷新令牌请求
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "刷新令牌不能为空")]
    public string RefreshToken { get; set; } = string.Empty;
    
    public string? ClientId { get; set; }
}

