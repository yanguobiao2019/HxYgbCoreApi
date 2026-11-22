using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Application.Services;
using Infrastructure.Services;

namespace Infrastructure.Services;

/// <summary>
/// Token 服务实现 - 提供 JWT Token 的生成和验证功能
/// </summary>
/// <remarks>
/// 实现 IdentityServer4 风格的 Token 管理：
/// 1. 使用 RSA 非对称加密签名（更安全，支持分布式验证）
/// 2. 支持 Access Token、Refresh Token 和 ID Token
/// 3. 符合 OAuth 2.0 和 OpenID Connect 规范
/// 4. 支持 Token 黑名单机制（通过 jti claim）
/// 
/// Token 类型说明：
/// - Access Token: 短期令牌，用于访问受保护资源（默认 60 分钟）
/// - Refresh Token: 长期令牌，用于获取新的 Access Token（默认 7 天）
/// - ID Token: OpenID Connect 身份令牌，包含用户身份信息
/// </remarks>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentialsService _signingCredentialsService;
    private readonly ILogger<TokenService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="configuration">配置服务，用于读取 JWT 相关配置</param>
    /// <param name="signingCredentialsService">签名凭证服务，提供 RSA 密钥</param>
    /// <param name="logger">日志服务</param>
    public TokenService(
        IConfiguration configuration, 
        SigningCredentialsService signingCredentialsService,
        ILogger<TokenService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _signingCredentialsService = signingCredentialsService ?? throw new ArgumentNullException(nameof(signingCredentialsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// 生成 JWT Access Token（访问令牌）
    /// </summary>
    /// <param name="claims">用户声明集合，包含用户身份信息</param>
    /// <param name="expirationMinutes">Token 过期时间（分钟），默认 60 分钟</param>
    /// <returns>JWT 格式的 Access Token 字符串</returns>
    /// <exception cref="ArgumentNullException">claims 为空时抛出</exception>
    /// <exception cref="InvalidOperationException">配置不正确时抛出</exception>
    /// <remarks>
    /// Access Token 结构：
    /// - Header: 包含算法类型（RS256）和 Token 类型（JWT）
    /// - Payload: 包含用户 Claims、标准 Claims（jti、iat、exp、nbf、iss、aud）
    /// - Signature: 使用 RSA 私钥签名，验证时使用公钥
    /// 
    /// 自动添加的标准 Claims：
    /// - jti (JWT ID): 唯一标识符，用于 Token 撤销/黑名单
    /// - iat (Issued At): Token 签发时间戳
    /// - exp (Expiration): Token 过期时间戳
    /// - nbf (Not Before): Token 生效时间戳
    /// - iss (Issuer): Token 签发者
    /// - aud (Audience): Token 目标受众
    /// </remarks>
    public string GenerateAccessToken(IEnumerable<Claim> claims, int expirationMinutes = 60)
    {
        if (claims == null)
            throw new ArgumentNullException(nameof(claims));
        
        if (expirationMinutes <= 0)
            throw new ArgumentException("过期时间必须大于 0", nameof(expirationMinutes));

        try
        {
            // 动态获取 Issuer 和 Audience（类似 IdentityServer4）
            // Issuer 是当前请求的基础 URL，如：https://localhost:5001
            var issuer = GetIssuer();
            var audience = issuer; // Audience 默认与 Issuer 相同

            _logger.LogDebug("Token Issuer: {Issuer}", issuer);

            // 获取 RSA 签名凭证
            var credentials = _signingCredentialsService.GetSigningCredentials();

            // 构建完整的 Claims 列表
            var claimsList = new List<Claim>(claims);
            
            // 添加 jti (JWT ID)：唯一标识符，用于 Token 黑名单功能
            // 当用户登出或撤销 Token 时，可以将 jti 加入黑名单
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            
            // 添加 iat (Issued At)：Token 签发时间，用于审计和验证
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Iat, 
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), 
                ClaimValueTypes.Integer64));

            var now = DateTime.UtcNow;
            
            // 创建 JWT Security Token
            var token = new JwtSecurityToken(
                issuer: issuer,                                    // 签发者
                audience: audience,                                // 目标受众
                claims: claimsList,                               // 用户声明
                notBefore: now,                                   // 生效时间（立即生效）
                expires: now.AddMinutes(expirationMinutes),      // 过期时间
                signingCredentials: credentials                   // RSA 签名凭证
            );

            // 将 Token 序列化为字符串
            var tokenString = _tokenHandler.WriteToken(token);
            
            _logger.LogDebug("成功生成 Access Token，过期时间：{ExpirationMinutes} 分钟", expirationMinutes);
            
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成 Access Token 失败");
            throw;
        }
    }
    
    /// <summary>
    /// 获取 Issuer（Token 签发者）
    /// </summary>
    /// <returns>当前请求的基础 URL</returns>
    /// <remarks>
    /// 类似 IdentityServer4 的做法：
    /// - 从当前 HTTP 请求动态获取 Issuer
    /// - 格式：{scheme}://{host}，例如 https://localhost:5001
    /// - 这样 Token 中的 iss 会自动适应不同的部署环境
    /// 
    /// 优点：
    /// 1. 无需配置固定的 Issuer
    /// 2. 自动适应开发、测试、生产环境
    /// 3. 支持多域名部署
    /// </remarks>
    private string GetIssuer()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var request = httpContext.Request;
            // 构建基础 URL：scheme://host[:port]
            var issuer = $"{request.Scheme}://{request.Host}";
            return issuer;
        }

        // 如果没有 HTTP 上下文（如后台任务），使用配置或默认值
        return _configuration["JwtSettings:Issuer"] ?? "OneAuthCenter";
    }

    /// <summary>
    /// 生成 Refresh Token（刷新令牌）
    /// </summary>
    /// <returns>Base64 编码的随机字符串</returns>
    /// <remarks>
    /// Refresh Token 特点：
    /// 1. 不包含用户信息，只是随机字符串
    /// 2. 需要存储在数据库中，用于验证和撤销
    /// 3. 有效期更长（通常 7-30 天）
    /// 4. 使用加密安全的随机数生成器（CSPRNG）
    /// 
    /// 安全性考虑：
    /// - 使用 32 字节（256 位）随机数，确保足够的熵值
    /// - 使用 RandomNumberGenerator（密码学安全）而非 Random
    /// - Token 值本身不包含任何可被解析的信息
    /// </remarks>
    public string GenerateRefreshToken()
    {
        try
        {
            // 生成 32 字节（256 位）的密码学安全随机数
            var randomNumber = new byte[32];
            
            // 使用密码学安全的随机数生成器（CSPRNG）
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            
            // 转换为 Base64 字符串，便于传输和存储
            var refreshToken = Convert.ToBase64String(randomNumber);
            
            _logger.LogDebug("成功生成 Refresh Token");
            
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成 Refresh Token 失败");
            throw;
        }
    }

    /// <summary>
    /// 生成 OpenID Connect ID Token（身份令牌）
    /// </summary>
    /// <param name="claims">用户声明集合</param>
    /// <param name="clientId">客户端 ID，作为 audience</param>
    /// <param name="nonce">可选的 nonce 值，用于防止重放攻击</param>
    /// <returns>JWT 格式的 ID Token 字符串</returns>
    /// <exception cref="ArgumentNullException">参数为空时抛出</exception>
    /// <remarks>
    /// ID Token 是 OpenID Connect 的核心概念：
    /// 1. 用途：向客户端应用证明用户身份，包含用户基本信息
    /// 2. 格式：JWT，客户端可以直接解析获取用户信息
    /// 3. 与 Access Token 的区别：
    ///    - Access Token: 用于访问 API，资源服务器验证
    ///    - ID Token: 用于身份认证，客户端应用使用
    /// 
    /// 标准 Claims（符合 OpenID Connect 规范）：
    /// - iss (Issuer): 令牌签发者
    /// - sub (Subject): 用户唯一标识符
    /// - aud (Audience): 令牌接收者（客户端 ID）
    /// - exp (Expiration): 过期时间
    /// - iat (Issued At): 签发时间
    /// - auth_time: 用户认证时间
    /// - nonce: 客户端提供的随机值，用于关联请求和响应，防止重放攻击
    /// 
    /// 可选 Claims（根据 scope 返回）：
    /// - name, email, phone_number, address 等用户信息
    /// </remarks>
    public string GenerateIdToken(IEnumerable<Claim> claims, string clientId, string? nonce = null)
    {
        if (claims == null)
            throw new ArgumentNullException(nameof(claims));
        
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentNullException(nameof(clientId));

        try
        {
            var idTokenClaims = new List<Claim>(claims);
            
            // aud (Audience): 指定 ID Token 的目标接收者（客户端）
            idTokenClaims.Add(new Claim("aud", clientId));
            
            // iat (Issued At): Token 签发时间戳
            idTokenClaims.Add(new Claim("iat", 
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64));
            
            // auth_time: 用户完成认证的时间戳
            // OpenID Connect 规范要求此字段，用于判断用户会话新鲜度
            idTokenClaims.Add(new Claim("auth_time", 
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64));

            // nonce: 客户端提供的随机值
            // 用途：将客户端会话与 ID Token 绑定，防止 Token 重放攻击
            if (!string.IsNullOrEmpty(nonce))
            {
                idTokenClaims.Add(new Claim("nonce", nonce));
            }

            _logger.LogDebug("生成 ID Token，ClientId: {ClientId}, Nonce: {Nonce}", 
                clientId, string.IsNullOrEmpty(nonce) ? "未提供" : "已提供");

            // ID Token 通常有效期较短（60 分钟）
            return GenerateAccessToken(idTokenClaims, 60);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成 ID Token 失败，ClientId: {ClientId}", clientId);
            throw;
        }
    }

    /// <summary>
    /// 验证 JWT Access Token 的有效性
    /// </summary>
    /// <param name="token">JWT Token 字符串</param>
    /// <returns>如果验证成功返回 ClaimsPrincipal，失败返回 null</returns>
    /// <remarks>
    /// 验证步骤（按顺序执行）：
    /// 1. 签名验证：使用 RSA 公钥验证 Token 签名是否正确
    /// 2. 格式验证：检查 Token 格式是否符合 JWT 规范
    /// 3. Issuer 验证：验证 Token 签发者是否匹配
    /// 4. Audience 验证：验证 Token 目标受众是否匹配
    /// 5. 生命周期验证：检查 Token 是否过期或未生效
    /// 
    /// 安全考虑：
    /// - ClockSkew 设置为 0：不允许时钟偏差，严格验证过期时间
    /// - 使用 RSA 非对称加密：验证服务只需要公钥，私钥安全性更高
    /// - 验证失败时返回 null 而非抛出异常：避免信息泄露
    /// 
    /// Token 验证失败的常见原因：
    /// - Token 已过期
    /// - Token 签名无效（被篡改）
    /// - Token 格式不正确
    /// - Issuer 或 Audience 不匹配
    /// - Token 尚未生效（nbf 时间未到）
    /// </remarks>
    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("尝试验证空 Token");
            return null;
        }

        try
        {
            // 读取 JWT 配置
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT 配置不完整：Issuer 或 Audience 缺失");
                return null;
            }

            // 获取 RSA 公钥用于验证签名
            var validationKey = _signingCredentialsService.GetValidationKey();
            
            // 配置 Token 验证参数
            var validationParameters = new TokenValidationParameters
            {
                // 签名验证
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = validationKey,
                
                // Issuer 验证（签发者）
                ValidateIssuer = true,
                ValidIssuer = issuer,
                
                // Audience 验证（目标受众）
                ValidateAudience = true,
                ValidAudience = audience,
                
                // 生命周期验证（过期时间）
                ValidateLifetime = true,
                
                // 时钟偏差设置为 0，严格验证过期时间
                // 生产环境可以设置小的偏差值（如 5 分钟）以应对服务器时钟不同步
                ClockSkew = TimeSpan.Zero,
                
                // 要求 Token 必须有过期时间
                RequireExpirationTime = true,
                
                // 要求 Token 必须有签名过期时间
                RequireSignedTokens = true
            };

            // 验证 Token 并返回 ClaimsPrincipal
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // 额外验证：确保是 JWT 格式
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger.LogWarning("Token 不是有效的 JWT 格式");
                return null;
            }
            
            // 额外验证：确保使用了正确的加密算法（防止 alg=none 攻击）
            if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Token 使用了不支持的加密算法：{Algorithm}", jwtToken.Header.Alg);
                return null;
            }
            
            _logger.LogDebug("Token 验证成功");
            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogInformation("Token 已过期：{Message}", ex.Message);
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning("Token 签名无效：{Message}", ex.Message);
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Token 验证失败：{Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token 验证过程中发生未预期的错误");
            return null;
        }
    }

    /// <summary>
    /// 从 Token 中提取用户 ID
    /// </summary>
    /// <param name="token">JWT Token 字符串</param>
    /// <returns>用户 ID，验证失败或无法解析时返回 null</returns>
    /// <remarks>
    /// 提取逻辑：
    /// 1. 先验证 Token 有效性
    /// 2. 从 Claims 中查找用户 ID
    ///    - 优先查找 ClaimTypes.NameIdentifier（标准 .NET Claim）
    ///    - 其次查找 "sub"（OpenID Connect 标准 Claim）
    /// 3. 将字符串 Claim 值转换为整数
    /// 
    /// 应用场景：
    /// - API 中间件识别当前用户
    /// - 审计日志记录操作用户
    /// - 权限检查时获取用户身份
    /// </remarks>
    public int? GetUserIdFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            // 先验证 Token 有效性
            var principal = ValidateAccessToken(token);
            if (principal == null)
            {
                _logger.LogDebug("无法从无效 Token 中提取用户 ID");
                return null;
            }

            // 查找用户 ID Claim（优先级：NameIdentifier > sub）
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) 
                             ?? principal.FindFirst("sub");
            
            if (userIdClaim == null)
            {
                _logger.LogWarning("Token 中未找到用户 ID Claim");
                return null;
            }

            // 尝试解析为整数
            if (int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogDebug("成功从 Token 中提取用户 ID: {UserId}", userId);
                return userId;
            }
            
            _logger.LogWarning("用户 ID Claim 值无法解析为整数：{Value}", userIdClaim.Value);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 Token 提取用户 ID 时发生错误");
            return null;
        }
    }
}

