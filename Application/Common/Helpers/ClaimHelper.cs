using System.Security.Claims;
using Domain.Entities;

namespace Application.Common.Helpers;

/// <summary>
/// Claims 构建辅助类
/// 用于统一管理用户声明的构建逻辑，避免代码重复
/// </summary>
public static class ClaimHelper
{
    /// <summary>
    /// 从用户实体构建标准 Claims 集合
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>包含用户信息的 Claims 列表</returns>
    /// <remarks>
    /// 生成的 Claims 包括：
    /// - NameIdentifier: 用户 ID
    /// - Name: 用户名
    /// - Email: 电子邮件
    /// - sub: OpenID Connect 标准的用户唯一标识符
    /// - GivenName: 全名（如果有）
    /// - Role: 用户角色（可能有多个）
    /// </remarks>
    public static List<Claim> BuildUserClaims(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        
        var claims = new List<Claim>
        {
            // 标准 Claims
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            
            // OpenID Connect 标准 Claim：subject（主体标识符）
            new Claim("sub", user.Id.ToString())
        };
        
        // 添加全名（如果存在）
        if (!string.IsNullOrEmpty(user.FullName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FullName));
        }
        
        // 添加电话号码（如果存在）
        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
        }
        
        // 添加角色 Claims
        AddRoleClaims(claims, user.Roles);
        
        return claims;
    }
    
    /// <summary>
    /// 为 OAuth 客户端构建 Claims
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <returns>客户端 Claims 列表</returns>
    public static List<Claim> BuildClientClaims(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentNullException(nameof(clientId));
        
        return new List<Claim>
        {
            new Claim("client_id", clientId),
            new Claim("sub", clientId) // OAuth 2.0 客户端凭证流程使用 client_id 作为 subject
        };
    }
    
    /// <summary>
    /// 添加角色 Claims
    /// </summary>
    /// <param name="claims">现有 Claims 列表</param>
    /// <param name="rolesString">角色字符串（逗号分隔）</param>
    private static void AddRoleClaims(List<Claim> claims, string rolesString)
    {
        if (string.IsNullOrEmpty(rolesString))
            return;
        
        var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var role in roles)
        {
            var trimmedRole = role.Trim();
            if (!string.IsNullOrEmpty(trimmedRole))
            {
                claims.Add(new Claim(ClaimTypes.Role, trimmedRole));
            }
        }
    }
    
    /// <summary>
    /// 提取用户 ID 从 ClaimsPrincipal
    /// </summary>
    /// <param name="principal">声明主体</param>
    /// <returns>用户 ID，如果无法解析则返回 null</returns>
    public static int? GetUserId(ClaimsPrincipal principal)
    {
        if (principal == null)
            return null;
        
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) 
                         ?? principal.FindFirst("sub");
        
        if (userIdClaim == null)
            return null;
        
        return int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }
    
    /// <summary>
    /// 从 ClaimsPrincipal 提取 scopes
    /// </summary>
    /// <param name="principal">声明主体</param>
    /// <returns>Scopes 列表</returns>
    public static HashSet<string> GetScopes(ClaimsPrincipal principal)
    {
        if (principal == null)
            return new HashSet<string>();
        
        // 从 Token 中提取所有 scope claims
        var scopes = principal.FindAll("scope")
            .Select(c => c.Value)
            .SelectMany(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        return scopes;
    }
    
    /// <summary>
    /// 检查是否包含指定的 scope
    /// </summary>
    /// <param name="principal">声明主体</param>
    /// <param name="scope">要检查的 scope</param>
    /// <returns>如果包含指定 scope 则返回 true</returns>
    public static bool HasScope(ClaimsPrincipal principal, string scope)
    {
        var scopes = GetScopes(principal);
        return scopes.Contains(scope);
    }
    
    /// <summary>
    /// 根据 scopes 过滤用户信息（用于 UserInfo 端点）
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="scopes">请求的 scopes</param>
    /// <returns>包含已授权信息的字典</returns>
    /// <remarks>
    /// OpenID Connect 标准 scopes 和对应的 claims：
    /// - openid: sub（必须）
    /// - profile: name, given_name, family_name, preferred_username 等
    /// - email: email, email_verified
    /// - phone: phone_number, phone_number_verified
    /// - address: address（地址信息）
    /// </remarks>
    public static Dictionary<string, object> BuildUserInfoByClaims(User user, HashSet<string> scopes)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        
        var userInfo = new Dictionary<string, object>();
        
        // sub（subject）是 OpenID Connect 必需的，始终返回
        userInfo["sub"] = user.Id.ToString();
        
        // profile scope：基本个人信息
        if (scopes.Contains("profile"))
        {
            userInfo["name"] = user.FullName ?? user.Username;
            userInfo["preferred_username"] = user.Username;
            
            if (!string.IsNullOrEmpty(user.FullName))
            {
                userInfo["given_name"] = user.FullName;
                // 如果有空格，拆分为 given_name 和 family_name
                var nameParts = user.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length > 0)
                {
                    userInfo["given_name"] = nameParts[0];
                }
                if (nameParts.Length > 1)
                {
                    userInfo["family_name"] = nameParts[1];
                }
            }
        }
        
        // email scope：电子邮件信息
        if (scopes.Contains("email"))
        {
            userInfo["email"] = user.Email;
            userInfo["email_verified"] = true; // 可以根据实际业务逻辑调整
        }
        
        // phone scope：电话号码信息
        if (scopes.Contains("phone"))
        {
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                userInfo["phone_number"] = user.PhoneNumber;
                userInfo["phone_number_verified"] = false; // 可以根据实际业务逻辑调整
            }
        }
        
        // address scope：地址信息（如果有）
        if (scopes.Contains("address"))
        {
            // 如果将来需要地址信息，可以在这里添加
            // userInfo["address"] = new { ... };
        }
        
        return userInfo;
    }
}

