using Application.DTOs.User;

namespace Application.Services;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取用户信息
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(int id);
    
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<UserDto?> GetUserByUsernameAsync(string username);
    
    /// <summary>
    /// 获取所有用户
    /// </summary>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    
    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task UpdateUserAsync(int id, UserDto userDto);
    
    /// <summary>
    /// 删除用户
    /// </summary>
    Task DeleteUserAsync(int id);
    
    /// <summary>
    /// 验证用户密码
    /// </summary>
    Task<bool> ValidatePasswordAsync(string username, string password);
    
    /// <summary>
    /// 更改密码
    /// </summary>
    Task ChangePasswordAsync(int userId, string oldPassword, string newPassword);
}

