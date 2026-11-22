namespace Application.DTOs.OAuth;

/// <summary>
/// JWKS (JSON Web Key Set) 响应
/// 用于发布公钥，让客户端验证 JWT Token 签名
/// </summary>
/// <remarks>
/// 符合 RFC 7517 - JSON Web Key (JWK) 规范
/// IdentityServer4 标准端点：/.well-known/jwks.json
/// 
/// JWKS 是一个包含多个公钥的集合，用于：
/// 1. 发布 Token 签名验证所需的公钥
/// 2. 支持密钥轮换（可以同时发布多个密钥）
/// 3. 让微服务和客户端独立验证 JWT Token
/// 
/// 使用场景：
/// - 微服务架构：各服务通过 JWKS 端点获取公钥，独立验证 Token
/// - 单页应用（SPA）：前端下载公钥验证 ID Token
/// - 移动应用：离线验证 Token 有效性
/// - API 网关：验证所有请求的 Access Token
/// 
/// 密钥轮换流程：
/// 1. 生成新密钥并添加到 JWKS（同时保留旧密钥）
/// 2. 使用新密钥签发新 Token
/// 3. 等待所有旧 Token 过期
/// 4. 从 JWKS 中移除旧密钥
/// 
/// 标准字段：
/// - keys: JWK 数组，包含一个或多个公钥
/// </remarks>
public class JwksResponse
{
    /// <summary>
    /// 密钥数组 - 包含一个或多个 JSON Web Key
    /// </summary>
    /// <remarks>
    /// 数组中的每个元素代表一个公钥
    /// - 单密钥场景：数组只有一个元素
    /// - 密钥轮换场景：数组包含新旧多个密钥
    /// 
    /// 客户端使用流程：
    /// 1. 从 JWT Header 中获取 kid（密钥 ID）
    /// 2. 在 keys 数组中查找匹配的 kid
    /// 3. 使用对应的公钥验证签名
    /// 
    /// 如果 JWT 没有 kid：
    /// - 尝试使用数组中的第一个密钥
    /// - 或者尝试所有密钥直到验证成功
    /// </remarks>
    public List<JsonWebKey> Keys { get; set; } = new();
}

