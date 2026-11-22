namespace Application.DTOs.OAuth;

/// <summary>
/// JSON Web Key (JWK) - 单个密钥信息
/// </summary>
/// <remarks>
/// RFC 7517 定义的标准格式
/// 
/// JWK 是一种 JSON 数据结构，用于表示加密密钥
/// 本类用于表示 RSA 公钥，用于 JWT Token 签名验证
/// 
/// 标准字段说明：
/// - kty: 密钥类型（Key Type）
/// - use: 密钥用途（Public Key Use）
/// - kid: 密钥标识符（Key ID）
/// - alg: 算法（Algorithm）
/// - n: RSA 模数（Modulus）
/// - e: RSA 指数（Exponent）
/// - x5c: X.509 证书链（X.509 Certificate Chain）
/// - x5t: X.509 证书 SHA-1 指纹（X.509 Certificate SHA-1 Thumbprint）
/// - x5t#S256: X.509 证书 SHA-256 指纹（X.509 Certificate SHA-256 Thumbprint）
/// </remarks>
public class JsonWebKey
{
    /// <summary>
    /// 密钥类型 (kty) - 对于 RSA 为 "RSA"
    /// </summary>
    /// <remarks>
    /// 可能的值：
    /// - "RSA": RSA 密钥
    /// - "EC": 椭圆曲线密钥
    /// - "oct": 对称密钥
    /// 本项目使用 RSA
    /// </remarks>
    public string Kty { get; set; } = "RSA";
    
    /// <summary>
    /// 密钥用途 (use) - "sig" 表示用于签名
    /// </summary>
    /// <remarks>
    /// 可能的值：
    /// - "sig": 签名（Signature）
    /// - "enc": 加密（Encryption）
    /// 本项目用于 JWT 签名
    /// </remarks>
    public string Use { get; set; } = "sig";
    
    /// <summary>
    /// 密钥 ID (kid) - 唯一标识符
    /// </summary>
    /// <remarks>
    /// 用途：
    /// - 当有多个密钥时，用于识别使用哪个密钥
    /// - JWT Header 中的 kid 字段对应此值
    /// - 支持密钥轮换场景
    /// </remarks>
    public string? Kid { get; set; }
    
    /// <summary>
    /// 算法 (alg) - "RS256" 表示 RSA + SHA256
    /// </summary>
    /// <remarks>
    /// 常用算法：
    /// - RS256: RSA + SHA256（推荐）
    /// - RS384: RSA + SHA384
    /// - RS512: RSA + SHA512
    /// - ES256: ECDSA + SHA256
    /// 本项目使用 RS256
    /// </remarks>
    public string Alg { get; set; } = "RS256";
    
    /// <summary>
    /// RSA 模数 (n) - Base64Url 编码
    /// </summary>
    /// <remarks>
    /// RSA 公钥的主要组成部分
    /// - 这是一个非常大的数字（通常 2048 位或 4096 位）
    /// - 使用 Base64Url 编码（URL 安全的 Base64）
    /// - 与指数 (e) 一起构成完整的 RSA 公钥
    /// </remarks>
    public string? N { get; set; }
    
    /// <summary>
    /// RSA 指数 (e) - Base64Url 编码
    /// </summary>
    /// <remarks>
    /// RSA 公钥的指数部分
    /// - 通常是 65537，Base64Url 编码后为 "AQAB"
    /// - 与模数 (n) 一起构成完整的 RSA 公钥
    /// - 此值在大多数情况下是固定的
    /// </remarks>
    public string? E { get; set; }
    
    /// <summary>
    /// X.509 证书链 (x5c) - 可选
    /// </summary>
    /// <remarks>
    /// DER 格式的 X.509 证书，Base64 编码
    /// 
    /// 用途：
    /// - 提供完整的证书链
    /// - 客户端可以验证证书的有效性
    /// - 支持证书信任链验证
    /// 
    /// 格式：
    /// - 数组的第一个元素是叶证书
    /// - 后续元素是中间证书和根证书
    /// </remarks>
    public List<string>? X5c { get; set; }
    
    /// <summary>
    /// X.509 证书 SHA-1 指纹 (x5t) - 可选
    /// </summary>
    /// <remarks>
    /// 证书的 SHA-1 哈希值，Base64Url 编码
    /// 用于快速识别证书
    /// </remarks>
    public string? X5t { get; set; }
    
    /// <summary>
    /// X.509 证书 SHA-256 指纹 (x5t#S256) - 可选
    /// </summary>
    /// <remarks>
    /// 证书的 SHA-256 哈希值，Base64Url 编码
    /// SHA-256 比 SHA-1 更安全，推荐使用
    /// </remarks>
    public string? X5tS256 { get; set; }
}

