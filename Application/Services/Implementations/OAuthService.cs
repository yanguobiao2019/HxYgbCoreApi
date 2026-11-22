using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.DTOs.Auth;
using Application.DTOs.OAuth;
using Domain.Entities;
using Domain.Repositories;

namespace Application.Services.Implementations;

/// <summary>
/// OAuth 2.0 服务实现
/// </summary>
public class OAuthService : IOAuthService
{
    private readonly IClientRepository _clientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuthorizationCodeRepository _authCodeRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserConsentRepository _consentRepository;
    private readonly ITokenService _tokenService;

    public OAuthService(
        IClientRepository clientRepository,
        IUserRepository userRepository,
        IAuthorizationCodeRepository authCodeRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUserConsentRepository consentRepository,
        ITokenService tokenService)
    {
        _clientRepository = clientRepository;
        _userRepository = userRepository;
        _authCodeRepository = authCodeRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _consentRepository = consentRepository;
        _tokenService = tokenService;
    }

    public async Task<string> AuthorizeAsync(AuthorizeRequest request, int userId)
    {
        // 验证请求
        if (!await ValidateAuthorizeRequestAsync(request))
        {
            throw new InvalidOperationException("授权请求无效");
        }

        var client = await _clientRepository.GetByClientIdAsync(request.ClientId);
        if (client == null)
        {
            throw new InvalidOperationException("客户端不存在");
        }

        // 生成授权码
        var code = GenerateAuthorizationCode();
        var authCode = new AuthorizationCode
        {
            Code = code,
            ClientId = client.Id,
            UserId = userId,
            RedirectUri = request.RedirectUri,
            Scopes = request.Scope,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Nonce = request.Nonce,
            State = request.State,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(client.AuthorizationCodeLifetime)
        };

        await _authCodeRepository.CreateAsync(authCode);

        // 构建重定向 URL
        var redirectUri = new UriBuilder(request.RedirectUri);
        var query = System.Web.HttpUtility.ParseQueryString(redirectUri.Query);
        query["code"] = code;
        
        if (!string.IsNullOrEmpty(request.State))
        {
            query["state"] = request.State;
        }

        redirectUri.Query = query.ToString();
        return redirectUri.ToString();
    }

    public async Task<bool> ValidateAuthorizeRequestAsync(AuthorizeRequest request)
    {
        // 验证客户端
        var client = await _clientRepository.GetByClientIdAsync(request.ClientId);
        if (client == null || !client.IsActive)
        {
            return false;
        }

        // 验证重定向 URI
        var redirectUris = client.RedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (!redirectUris.Contains(request.RedirectUri))
        {
            return false;
        }

        // 验证授权类型
        var allowedGrantTypes = client.AllowedGrantTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (request.ResponseType == "code" && !allowedGrantTypes.Contains("authorization_code"))
        {
            return false;
        }

        // 验证 PKCE
        if (client.RequirePkce && string.IsNullOrEmpty(request.CodeChallenge))
        {
            return false;
        }

        return true;
    }

    public async Task<TokenResponse> TokenAsync(TokenRequest request)
    {
        return request.GrantType switch
        {
            "authorization_code" => await HandleAuthorizationCodeGrantAsync(request),
            "client_credentials" => await HandleClientCredentialsGrantAsync(request),
            "password" => await HandlePasswordGrantAsync(request),
            "refresh_token" => await HandleRefreshTokenGrantAsync(request),
            _ => throw new InvalidOperationException($"不支持的授权类型: {request.GrantType}")
        };
    }

    public async Task SaveConsentAsync(ConsentRequest request, int userId)
    {
        var client = await _clientRepository.GetByClientIdAsync(request.ClientId);
        if (client == null)
        {
            throw new InvalidOperationException("客户端不存在");
        }

        var existingConsent = await _consentRepository.GetByUserAndClientAsync(userId, client.Id);

        if (existingConsent != null)
        {
            existingConsent.Scopes = string.Join(",", request.GrantedScopes);
            existingConsent.ExpiresAt = request.RememberConsent ? DateTime.UtcNow.AddYears(1) : null;
            await _consentRepository.UpdateAsync(existingConsent);
        }
        else
        {
            var consent = new UserConsent
            {
                UserId = userId,
                ClientId = client.Id,
                Scopes = string.Join(",", request.GrantedScopes),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = request.RememberConsent ? DateTime.UtcNow.AddYears(1) : null
            };
            await _consentRepository.CreateAsync(consent);
        }
    }

    public async Task<string[]?> GetConsentedScopesAsync(int userId, string clientId)
    {
        var client = await _clientRepository.GetByClientIdAsync(clientId);
        if (client == null) return null;

        var consent = await _consentRepository.GetByUserAndClientAsync(userId, client.Id);
        if (consent == null) return null;

        if (consent.ExpiresAt.HasValue && consent.ExpiresAt.Value < DateTime.UtcNow)
        {
            return null;
        }

        return consent.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    // 授权码授权流程
    private async Task<TokenResponse> HandleAuthorizationCodeGrantAsync(TokenRequest request)
    {
        var authCode = await _authCodeRepository.GetByCodeAsync(request.Code!);
        
        if (authCode == null || authCode.IsUsed)
        {
            throw new UnauthorizedAccessException("授权码无效或已使用");
        }

        if (authCode.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("授权码已过期");
        }

        var client = await _clientRepository.GetByClientIdAsync(request.ClientId);
        if (client == null || authCode.ClientId != client.Id)
        {
            throw new UnauthorizedAccessException("客户端不匹配");
        }

        if (authCode.RedirectUri != request.RedirectUri)
        {
            throw new UnauthorizedAccessException("重定向 URI 不匹配");
        }

        // 验证 PKCE
        if (!string.IsNullOrEmpty(authCode.CodeChallenge))
        {
            if (!VerifyPkce(request.CodeVerifier!, authCode.CodeChallenge, authCode.CodeChallengeMethod!))
            {
                throw new UnauthorizedAccessException("PKCE 验证失败");
            }
        }

        // 标记授权码为已使用
        authCode.IsUsed = true;
        authCode.UsedAt = DateTime.UtcNow;
        await _authCodeRepository.UpdateAsync(authCode);

        // 获取用户信息
        var user = await _userRepository.GetByIdAsync(authCode.UserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("用户不存在");
        }

        // 生成令牌
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("sub", user.Id.ToString()),
            new Claim("client_id", client.ClientId)
        };
        
        // 添加 scope claims（OpenID Connect 要求）
        if (!string.IsNullOrEmpty(authCode.Scopes))
        {
            var scopes = authCode.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var scope in scopes)
            {
                claims.Add(new Claim("scope", scope));
            }
        }

        var accessToken = _tokenService.GenerateAccessToken(claims, client.AccessTokenLifetime / 60);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ClientId = client.Id,
            ExpiresAt = DateTime.UtcNow.AddSeconds(client.RefreshTokenLifetime),
            Scopes = authCode.Scopes
        };
        await _refreshTokenRepository.CreateAsync(refreshToken);

        var response = new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = client.AccessTokenLifetime,
            TokenType = "Bearer",
            Scope = authCode.Scopes
        };

        // 如果请求了 openid，生成 ID Token
        if (authCode.Scopes?.Contains("openid") == true)
        {
            response.IdToken = _tokenService.GenerateIdToken(claims, client.ClientId, authCode.Nonce);
        }

        return response;
    }

    // 客户端凭证授权流程
    private async Task<TokenResponse> HandleClientCredentialsGrantAsync(TokenRequest request)
    {
        if (string.IsNullOrEmpty(request.ClientSecret))
        {
            throw new UnauthorizedAccessException("客户端密钥不能为空");
        }

        if (!await _clientRepository.ValidateClientAsync(request.ClientId, request.ClientSecret))
        {
            throw new UnauthorizedAccessException("客户端凭证无效");
        }

        var client = await _clientRepository.GetByClientIdAsync(request.ClientId);
        
        var claims = new List<Claim>
        {
            new Claim("client_id", request.ClientId),
            new Claim("sub", request.ClientId)
        };
        
        // 添加 scope claims（Client Credentials Flow）
        var scope = request.Scope ?? client!.AllowedScopes;
        if (!string.IsNullOrEmpty(scope))
        {
            var scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in scopes)
            {
                claims.Add(new Claim("scope", s));
            }
        }

        var accessToken = _tokenService.GenerateAccessToken(claims, client!.AccessTokenLifetime / 60);

        return new TokenResponse
        {
            AccessToken = accessToken,
            ExpiresIn = client.AccessTokenLifetime,
            TokenType = "Bearer",
            Scope = request.Scope ?? client.AllowedScopes
        };
    }

    // 密码授权流程
    private async Task<TokenResponse> HandlePasswordGrantAsync(TokenRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username!);
        if (user == null)
        {
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        // 这里简化了密码验证，实际应该使用哈希验证
        var client = await _clientRepository.GetByClientIdAsync(request.ClientId);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("sub", user.Id.ToString()),
            new Claim("client_id", request.ClientId)
        };
        
        // 添加 scope claims（Password Grant Flow）
        var scope = request.Scope ?? client!.AllowedScopes;
        if (!string.IsNullOrEmpty(scope))
        {
            var scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in scopes)
            {
                claims.Add(new Claim("scope", s));
            }
        }

        var accessToken = _tokenService.GenerateAccessToken(claims, client!.AccessTokenLifetime / 60);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ClientId = client.Id,
            ExpiresAt = DateTime.UtcNow.AddSeconds(client.RefreshTokenLifetime),
            Scopes = scope
        };
        await _refreshTokenRepository.CreateAsync(refreshToken);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = client.AccessTokenLifetime,
            TokenType = "Bearer",
            Scope = request.Scope ?? client.AllowedScopes
        };
    }

    // 刷新令牌授权流程
    private async Task<TokenResponse> HandleRefreshTokenGrantAsync(TokenRequest request)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken!);
        
        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("刷新令牌无效或已过期");
        }

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("用户不存在");
        }

        var client = await _clientRepository.GetByClientIdAsync(request.ClientId);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("sub", user.Id.ToString()),
            new Claim("client_id", request.ClientId)
        };
        
        // 添加 scope claims（Refresh Token Flow）
        if (!string.IsNullOrEmpty(refreshToken.Scopes))
        {
            var scopes = refreshToken.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var scope in scopes)
            {
                claims.Add(new Claim("scope", scope));
            }
        }

        var accessToken = _tokenService.GenerateAccessToken(claims, client!.AccessTokenLifetime / 60);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = request.RefreshToken,
            ExpiresIn = client.AccessTokenLifetime,
            TokenType = "Bearer",
            Scope = refreshToken.Scopes
        };
    }

    private string GenerateAuthorizationCode()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private bool VerifyPkce(string codeVerifier, string codeChallenge, string codeChallengeMethod)
    {
        if (codeChallengeMethod == "plain")
        {
            return codeVerifier == codeChallenge;
        }
        else if (codeChallengeMethod == "S256")
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var computedChallenge = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
            return computedChallenge == codeChallenge;
        }
        return false;
    }
}

