namespace Application.Common.Constants;

/// <summary>
/// 认证相关常量
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// 默认 Token 配置
    /// </summary>
    public static class TokenDefaults
    {
        /// <summary>
        /// 默认访问令牌过期时间（分钟）
        /// </summary>
        public const int DefaultAccessTokenExpirationMinutes = 60;
        
        /// <summary>
        /// 默认刷新令牌过期时间（天）
        /// </summary>
        public const int DefaultRefreshTokenExpirationDays = 7;
        
        /// <summary>
        /// 默认 ID Token 过期时间（分钟）
        /// </summary>
        public const int DefaultIdTokenExpirationMinutes = 60;
        
        /// <summary>
        /// 刷新令牌字节长度
        /// </summary>
        public const int RefreshTokenByteLength = 32;
    }
    
    /// <summary>
    /// Scope 相关常量
    /// </summary>
    public static class Scopes
    {
        public const string OpenId = "openid";
        public const string Profile = "profile";
        public const string Email = "email";
        public const string Phone = "phone";
        public const string Address = "address";
        
        /// <summary>
        /// 默认 Scope 组合
        /// </summary>
        public const string DefaultScopes = "openid profile email";
    }
    
    /// <summary>
    /// 默认角色
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Guest = "Guest";
    }
    
    /// <summary>
    /// BCrypt 工作因子（越高越安全但越慢，推荐 11-12）
    /// </summary>
    public const int BcryptWorkFactor = 11;
}

