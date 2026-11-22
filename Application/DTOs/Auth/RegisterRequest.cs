using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

/// <summary>
/// 注册请求
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在 3-50 个字符之间")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在 6-100 个字符之间")]
    public string Password { get; set; } = string.Empty;
    
    public string? FullName { get; set; }
    
    public string? PhoneNumber { get; set; }
}

