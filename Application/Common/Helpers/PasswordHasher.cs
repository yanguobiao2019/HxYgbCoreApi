using Application.Common.Constants;

namespace Application.Common.Helpers;

/// <summary>
/// 密码哈希辅助类
/// 使用 BCrypt 算法进行安全的密码哈希和验证
/// </summary>
/// <remarks>
/// BCrypt 优势：
/// 1. 自动加盐：每次哈希都使用随机盐值，防止彩虹表攻击
/// 2. 工作因子可调：可以随着硬件性能提升而提高安全性
/// 3. 慢速哈希：故意设计为计算密集型，防止暴力破解
/// 4. 行业标准：被广泛认可和使用的密码哈希算法
/// </remarks>
public static class PasswordHasher
{
    /// <summary>
    /// 对密码进行哈希处理
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>BCrypt 哈希后的密码字符串</returns>
    /// <exception cref="ArgumentNullException">密码为空时抛出</exception>
    /// <remarks>
    /// 使用 BCrypt 算法，工作因子为 11（默认）
    /// 工作因子越高，哈希越安全但计算时间越长
    /// 推荐值：11-12（生产环境）
    /// </remarks>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password), "密码不能为空");
        
        // 使用 BCrypt 进行哈希，工作因子为 11
        // BCrypt 会自动生成随机盐值并将其包含在返回的哈希字符串中
        return BCrypt.Net.BCrypt.HashPassword(password, AuthConstants.BcryptWorkFactor);
    }
    
    /// <summary>
    /// 验证密码是否与哈希值匹配
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="hashedPassword">BCrypt 哈希后的密码</param>
    /// <returns>如果密码匹配返回 true，否则返回 false</returns>
    /// <remarks>
    /// BCrypt 验证过程：
    /// 1. 从哈希字符串中提取盐值和工作因子
    /// 2. 使用相同的参数对输入密码进行哈希
    /// 3. 比较两个哈希值是否相同（常量时间比较，防止时序攻击）
    /// </remarks>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password))
            return false;
        
        if (string.IsNullOrEmpty(hashedPassword))
            return false;
        
        try
        {
            // BCrypt.Verify 会自动从 hashedPassword 中提取盐值并进行验证
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            // 如果哈希格式不正确或发生其他错误，返回 false
            return false;
        }
    }
    
    /// <summary>
    /// 检查密码哈希是否需要重新哈希（工作因子升级）
    /// </summary>
    /// <param name="hashedPassword">现有的哈希密码</param>
    /// <returns>如果需要重新哈希返回 true</returns>
    /// <remarks>
    /// 当系统的工作因子升级时，旧密码应该在用户下次登录时重新哈希
    /// 这样可以逐步提高系统的安全性
    /// </remarks>
    public static bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
            return true;
        
        try
        {
            // 检查当前哈希使用的工作因子是否低于配置的工作因子
            return !BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, AuthConstants.BcryptWorkFactor);
        }
        catch
        {
            return true;
        }
    }
}

