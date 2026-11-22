using Application.DTOs.Auth;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace HxYgbCoreWebApi.Controllers
{
    /// <summary>
    /// 认证控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] Application.DTOs.Auth.LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "登录失败: {Username}", request.Username);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录时发生错误");
                return StatusCode(500, new { message = "登录失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] Application.DTOs.Auth.RegisterRequest request)
        {
            try
            {
                var user = await _authService.RegisterAsync(request);
                return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "注册失败: {Username}", request.Username);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册时发生错误");
                return StatusCode(500, new { message = "注册失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 刷新令牌
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "刷新令牌失败");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新令牌时发生错误");
                return StatusCode(500, new { message = "刷新令牌失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 撤销刷新令牌
        /// </summary>
        [HttpPost("revoke")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                await _authService.RevokeTokenAsync(request.RefreshToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销令牌时发生错误");
                return StatusCode(500, new { message = "撤销令牌失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 撤销 Access Token（将 JWT 加入黑名单）
        /// </summary>
        [HttpPost("revoke-access-token")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RevokeAccessToken()
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { message = "未提供 Token" });
                }

                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // 获取 jti, userId, exp
                var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var exp = jwtToken.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp)?.Value;

                if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return BadRequest(new { message = "无效的 Token" });
                }

                // 计算过期时间
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp!)).UtcDateTime;

                // 添加到黑名单
                var revokedTokenRepository = HttpContext.RequestServices.GetRequiredService<Domain.Repositories.IRevokedTokenRepository>();
                var tokenHash = ComputeTokenHash(token);

                await revokedTokenRepository.AddAsync(new Domain.Entities.RevokedToken
                {
                    TokenId = jti,
                    UserId = userId,
                    TokenHash = tokenHash,
                    RevokedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    Reason = "用户主动撤销"
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销 Access Token 时发生错误");
                return StatusCode(500, new { message = "撤销 Token 失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 登出（撤销用户所有令牌）
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "无效的用户身份" });
                }

                await _authService.RevokeAllUserTokensAsync(userId);

                // 同时撤销当前 Access Token
                var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var jwtToken = handler.ReadJwtToken(token);
                        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                        var exp = jwtToken.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp)?.Value;

                        if (!string.IsNullOrEmpty(jti) && !string.IsNullOrEmpty(exp))
                        {
                            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).UtcDateTime;
                            var revokedTokenRepository = HttpContext.RequestServices.GetRequiredService<Domain.Repositories.IRevokedTokenRepository>();
                            var tokenHash = ComputeTokenHash(token);

                            await revokedTokenRepository.AddAsync(new Domain.Entities.RevokedToken
                            {
                                TokenId = jti,
                                UserId = userId,
                                TokenHash = tokenHash,
                                RevokedAt = DateTime.UtcNow,
                                ExpiresAt = expiresAt,
                                Reason = "用户登出"
                            });
                        }
                    }
                    catch
                    {
                        // 忽略 Access Token 撤销失败，继续处理 Refresh Token
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出时发生错误");
                return StatusCode(500, new { message = "登出失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 计算 Token 的 SHA256 哈希
        /// </summary>
        private static string ComputeTokenHash(string token)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}
