using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Application.Common.Constants;
using Application.Common.Helpers;
using Application.DTOs.Auth;
using Application.DTOs.User;
using Domain.Entities;
using Domain.Repositories;

namespace Application.Services.Implementations;

/// <summary>
/// 认证服务实现 - 提供用户认证和令牌管理功能
/// </summary>
/// <remarks>
/// 核心功能：
/// 1. 用户登录：验证凭据并生成令牌
/// 2. 用户注册：创建新用户账户
/// 3. 令牌刷新：使用 Refresh Token 获取新的 Access Token
/// 4. 令牌撤销：撤销单个或全部用户令牌
/// 
/// 安全特性：
/// - 使用 BCrypt 进行密码哈希（工作因子 11）
/// - 支持令牌轮换（Token Rotation）
/// - 自动检测和处理被禁用的用户
/// - 记录用户最后登录时间
/// </remarks>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IClientRepository clientRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求，包含用户名和密码</param>
    /// <returns>包含 Access Token 和 Refresh Token 的响应</returns>
    /// <exception cref="UnauthorizedAccessException">用户名或密码错误，或用户被禁用</exception>
    /// <remarks>
    /// 登录流程：
    /// 1. 验证用户凭据（用户名 + 密码）
    /// 2. 检查用户状态（是否被禁用）
    /// 3. 更新最后登录时间
    /// 4. 构建用户 Claims
    /// 5. 生成 Access Token 和 Refresh Token
    /// 6. 将 Refresh Token 存储到数据库
    /// 
    /// 安全考虑：
    /// - 使用 BCrypt 验证密码（防止彩虹表攻击）
    /// - 不在错误消息中区分"用户不存在"和"密码错误"（防止用户名枚举）
    /// - 检查用户是否被禁用
    /// - 支持客户端特定的 Token 配置
    /// </remarks>
    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            _logger.LogInformation("用户尝试登录：{Username}", request.Username);

            // 1. 查找用户
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            
            // 2. 验证用户存在性和密码正确性
            // 注意：为了安全，统一返回"用户名或密码错误"，不区分具体是哪个错误
            if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("登录失败：用户名或密码错误，Username: {Username}", request.Username);
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 3. 检查用户是否被禁用
            if (!user.IsActive)
            {
                _logger.LogWarning("登录失败：用户已被禁用，UserId: {UserId}", user.Id);
                throw new UnauthorizedAccessException("用户已被禁用");
            }

            // 4. 更新最后登录时间（审计用途）
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            // 5. 构建用户 Claims（使用辅助方法，避免代码重复）
            var claims = ClaimHelper.BuildUserClaims(user);
            
            // 添加默认 scopes（用于直接登录场景）
            // 默认授予 openid, profile, email 权限
            var defaultScopes = new[] { "openid", "profile", "email" };
            foreach (var scope in defaultScopes)
            {
                claims.Add(new System.Security.Claims.Claim("scope", scope));
            }
            
            // 如果用户有电话号码，添加 phone scope
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                claims.Add(new System.Security.Claims.Claim("scope", "phone"));
            }

            // 6. 生成 Access Token（JWT）
            var accessToken = _tokenService.GenerateAccessToken(claims);

            // 7. 生成 Refresh Token（随机字符串）
            var refreshTokenValue = _tokenService.GenerateRefreshToken();
            
            // 8. 确定 Refresh Token 过期时间
            // 如果指定了客户端，使用客户端配置的过期时间；否则使用默认值
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(AuthConstants.TokenDefaults.DefaultRefreshTokenExpirationDays);
            int? clientDbId = null;

            if (!string.IsNullOrEmpty(request.ClientId))
            {
                var client = await _clientRepository.GetByClientIdAsync(request.ClientId);
                if (client != null)
                {
                    clientDbId = client.Id;
                    // 使用客户端自定义的 Refresh Token 生命周期
                    refreshTokenExpiresAt = DateTime.UtcNow.AddSeconds(client.RefreshTokenLifetime);
                    _logger.LogDebug("使用客户端 {ClientId} 的 Refresh Token 配置，有效期：{Lifetime} 秒", 
                        request.ClientId, client.RefreshTokenLifetime);
                }
            }

            // 9. 创建 Refresh Token 实体并保存到数据库
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ClientId = clientDbId,
                ExpiresAt = refreshTokenExpiresAt,
                Scopes = AuthConstants.Scopes.DefaultScopes,
                CreatedAt = DateTime.UtcNow
            };
            await _refreshTokenRepository.CreateAsync(refreshToken);

            _logger.LogInformation("用户登录成功：UserId: {UserId}, Username: {Username}", user.Id, user.Username);

            // 10. 返回令牌响应
            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresIn = AuthConstants.TokenDefaults.DefaultAccessTokenExpirationMinutes * 60, // 转换为秒
                TokenType = "Bearer",
                Scope = AuthConstants.Scopes.DefaultScopes
            };
        }
        catch (UnauthorizedAccessException)
        {
            // 重新抛出授权异常
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录过程中发生未预期的错误");
            throw;
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="request">注册请求，包含用户信息</param>
    /// <returns>新创建的用户DTO</returns>
    /// <exception cref="InvalidOperationException">用户名或邮箱已被使用</exception>
    /// <remarks>
    /// 注册流程：
    /// 1. 检查用户名和邮箱是否已存在
    /// 2. 使用 BCrypt 哈希密码
    /// 3. 创建用户实体
    /// 4. 保存到数据库
    /// 5. 返回用户信息（不包含密码）
    /// 
    /// 安全考虑：
    /// - 密码使用 BCrypt 哈希（工作因子 11）
    /// - 检查用户名和邮箱唯一性
    /// - 新用户默认角色为 "User"
    /// - 新用户默认为激活状态
    /// </remarks>
    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            _logger.LogInformation("尝试注册新用户：{Username}, {Email}", request.Username, request.Email);

            // 1. 检查用户名或邮箱是否已存在
            if (await _userRepository.ExistsAsync(request.Username, request.Email))
            {
                _logger.LogWarning("注册失败：用户名或邮箱已存在，Username: {Username}, Email: {Email}", 
                    request.Username, request.Email);
                throw new InvalidOperationException("用户名或邮箱已被使用");
            }

            // 2. 使用 BCrypt 哈希密码
            var passwordHash = PasswordHasher.HashPassword(request.Password);

            // 3. 创建用户实体
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Roles = AuthConstants.Roles.User // 新用户默认为普通用户角色
            };

            // 4. 保存到数据库
            var createdUser = await _userRepository.CreateAsync(user);

            _logger.LogInformation("用户注册成功：UserId: {UserId}, Username: {Username}", 
                createdUser.Id, createdUser.Username);

            // 5. 返回用户 DTO（不包含敏感信息）
            return new UserDto
            {
                Id = createdUser.Id,
                Username = createdUser.Username,
                Email = createdUser.Email,
                FullName = createdUser.FullName,
                PhoneNumber = createdUser.PhoneNumber,
                IsActive = createdUser.IsActive,
                Roles = createdUser.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries),
                CreatedAt = createdUser.CreatedAt,
                LastLoginAt = createdUser.LastLoginAt
            };
        }
        catch (InvalidOperationException)
        {
            // 重新抛出业务逻辑异常
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册过程中发生未预期的错误");
            throw;
        }
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="request">刷新令牌请求</param>
    /// <returns>新的令牌响应</returns>
    /// <exception cref="UnauthorizedAccessException">Refresh Token 无效、已撤销或已过期</exception>
    /// <remarks>
    /// 令牌刷新流程：
    /// 1. 验证 Refresh Token 有效性
    /// 2. 检查 Token 是否已被撤销
    /// 3. 检查 Token 是否已过期
    /// 4. 验证用户状态
    /// 5. 生成新的 Access Token
    /// 6. 实施令牌轮换（Token Rotation）：撤销旧的 Refresh Token，生成新的
    /// 
    /// 令牌轮换（Token Rotation）：
    /// - 安全最佳实践：每次使用 Refresh Token 时都生成新的
    /// - 好处：即使 Refresh Token 被窃取，攻击者只能使用一次
    /// - 实现：将旧 Token 标记为已撤销，创建新 Token
    /// 
    /// 安全考虑：
    /// - 检查 Token 是否在数据库中存在
    /// - 检查 Token 是否已被撤销
    /// - 检查 Token 是否已过期
    /// - 检查用户是否仍然激活
    /// - 实施令牌轮换机制
    /// </remarks>
    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            _logger.LogDebug("尝试刷新令牌");

            // 1. 从数据库查找 Refresh Token
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
            
            // 2. 验证 Token 存在性
            if (refreshToken == null)
            {
                _logger.LogWarning("刷新令牌失败：Token 不存在");
                throw new UnauthorizedAccessException("刷新令牌无效");
            }

            // 3. 检查 Token 是否已被撤销
            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning("刷新令牌失败：Token 已被撤销，TokenId: {TokenId}, UserId: {UserId}", 
                    refreshToken.Id, refreshToken.UserId);
                throw new UnauthorizedAccessException("刷新令牌已被撤销");
            }

            // 4. 检查 Token 是否已过期
            if (refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("刷新令牌失败：Token 已过期，TokenId: {TokenId}, ExpiresAt: {ExpiresAt}", 
                    refreshToken.Id, refreshToken.ExpiresAt);
                throw new UnauthorizedAccessException("刷新令牌已过期");
            }

            // 5. 获取用户信息并验证用户状态
            var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("刷新令牌失败：用户不存在或已被禁用，UserId: {UserId}", refreshToken.UserId);
                throw new UnauthorizedAccessException("用户不存在或已被禁用");
            }

            // 6. 构建用户 Claims
            var claims = ClaimHelper.BuildUserClaims(user);
            
            // 添加 scopes（从原 Refresh Token 中继承）
            if (!string.IsNullOrEmpty(refreshToken.Scopes))
            {
                var scopes = refreshToken.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var scope in scopes)
                {
                    claims.Add(new System.Security.Claims.Claim("scope", scope));
                }
            }

            // 7. 生成新的 Access Token
            var accessToken = _tokenService.GenerateAccessToken(claims);

            // 8. 实施令牌轮换（Token Rotation）
            // 安全最佳实践：撤销旧的 Refresh Token，生成新的
            _logger.LogDebug("执行令牌轮换，撤销旧 Token: {TokenId}", refreshToken.Id);
            
            // 撤销旧 Token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            // 生成新 Refresh Token
            var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenValue,
                UserId = user.Id,
                ClientId = refreshToken.ClientId,
                ExpiresAt = DateTime.UtcNow.AddDays(AuthConstants.TokenDefaults.DefaultRefreshTokenExpirationDays),
                Scopes = refreshToken.Scopes,
                CreatedAt = DateTime.UtcNow
            };
            await _refreshTokenRepository.CreateAsync(newRefreshToken);

            _logger.LogInformation("令牌刷新成功：UserId: {UserId}", user.Id);

            // 9. 返回新令牌
            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshTokenValue,
                ExpiresIn = AuthConstants.TokenDefaults.DefaultAccessTokenExpirationMinutes * 60,
                TokenType = "Bearer",
                Scope = refreshToken.Scopes
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新令牌过程中发生未预期的错误");
            throw;
        }
    }

    /// <summary>
    /// 撤销指定的 Refresh Token
    /// </summary>
    /// <param name="refreshToken">要撤销的 Refresh Token 字符串</param>
    /// <remarks>
    /// 使用场景：
    /// - 用户主动撤销某个设备的登录
    /// - 安全策略要求撤销可疑 Token
    /// - 用户更改密码后撤销所有旧 Token
    /// </remarks>
    public async Task RevokeTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("尝试撤销空 Token");
            return;
        }

        try
        {
            _logger.LogDebug("撤销 Refresh Token");
            await _refreshTokenRepository.RevokeAsync(refreshToken);
            _logger.LogInformation("Refresh Token 已撤销");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销 Refresh Token 时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 撤销用户的所有 Refresh Token
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <remarks>
    /// 使用场景：
    /// - 用户登出（撤销所有设备）
    /// - 检测到可疑活动
    /// - 用户更改密码
    /// - 管理员强制下线用户
    /// 
    /// 注意：此操作会使用户在所有设备上的登录失效
    /// </remarks>
    public async Task RevokeAllUserTokensAsync(int userId)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("尝试撤销无效用户的 Token，UserId: {UserId}", userId);
            return;
        }

        try
        {
            _logger.LogInformation("撤销用户所有 Refresh Token，UserId: {UserId}", userId);
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
            _logger.LogInformation("用户所有 Refresh Token 已撤销，UserId: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销用户所有 Refresh Token 时发生错误，UserId: {UserId}", userId);
            throw;
        }
    }
}
