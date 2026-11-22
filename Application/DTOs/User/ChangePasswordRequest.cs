namespace Application.DTOs.User;

/// <summary>
/// 修改密码请求
/// </summary>
public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

