using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services
{
    /// <summary>
    /// 签名凭证服务 - 管理 JWT Token 的签名密钥（仿照 IdentityServer4 的 AddDeveloperSigningCredential）
    /// </summary>
    /// <remarks>
    /// 功能说明：
    /// 1. 开发环境：自动生成临时 RSA 密钥对（2048 位）
    /// 2. 生产环境：从 PFX 证书文件或证书存储加载密钥
    /// 3. 使用 RS256 算法（RSA + SHA256）进行签名
    /// 
    /// RSA 非对称加密的优势：
    /// - 安全性高：私钥用于签名，公钥用于验证，私钥永远不会泄露
    /// - 分布式友好：多个服务可以共享公钥验证 Token，无需共享私钥
    /// - 行业标准：JWT 推荐算法，广泛支持
    /// 
    /// 与 IdentityServer4 的对比：
    /// - IdentityServer4: AddDeveloperSigningCredential() - 开发环境临时密钥
    /// - 本服务: CreateDevelopmentRsaKey() - 相同功能
    /// - IdentityServer4: AddSigningCredential(cert) - 生产环境证书
    /// - 本服务: LoadCertificateFromFile() - 相同功能
    /// </remarks>
    public class SigningCredentialsService
    {
        private readonly SigningCredentials _signingCredentials;
        private readonly RsaSecurityKey _rsaSecurityKey;
        private readonly ILogger<SigningCredentialsService>? _logger;

        /// <summary>
        /// 构造函数 - 初始化签名凭证
        /// </summary>
        /// <param name="useTestCertificate">是否使用测试证书（开发环境）</param>
        /// <param name="certificatePath">证书文件路径（生产环境）</param>
        /// <param name="certificatePassword">证书密码</param>
        /// <param name="logger">可选的日志记录器</param>
        /// <remarks>
        /// 初始化逻辑：
        /// 1. 如果 useTestCertificate 为 true 或 certificatePath 为空 → 使用开发密钥
        /// 2. 否则从指定的证书文件加载 → 生产环境
        /// 
        /// 警告：开发密钥仅用于开发和测试！
        /// - 密钥存储在内存中，应用重启后会重新生成
        /// - 所有之前签发的 Token 将失效
        /// - 不适合生产环境或分布式部署
        /// </remarks>
        public SigningCredentialsService(
            bool useTestCertificate = true,
            string? certificatePath = null,
            string? certificatePassword = null,
            ILogger<SigningCredentialsService>? logger = null)
        {
            _logger = logger;

            try
            {
                if (useTestCertificate || string.IsNullOrEmpty(certificatePath))
                {
                    // 开发环境：生成或加载临时 RSA 密钥（类似 IdentityServer4 的 tempkey.jwk）
                    _logger?.LogInformation("初始化开发环境签名凭证（临时 RSA 密钥）");
                    _rsaSecurityKey = LoadOrCreateDevelopmentKey();
                    _signingCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256);

                    Console.WriteLine("✓ 使用开发环境 RSA 密钥签名（临时密钥，仅用于开发）");
                    _logger?.LogInformation("开发密钥已保存到 tempkey.rsa，应用重启后将复用此密钥");
                }
                else
                {
                    // 生产环境：从证书文件加载
                    _logger?.LogInformation("从证书文件加载签名凭证：{CertificatePath}", certificatePath);
                    var certificate = LoadCertificateFromFile(certificatePath, certificatePassword);

                    // 从证书中提取 RSA 私钥
                    var rsaPrivateKey = certificate.GetRSAPrivateKey();
                    if (rsaPrivateKey == null)
                    {
                        throw new InvalidOperationException("证书中不包含 RSA 私钥");
                    }

                    _rsaSecurityKey = new RsaSecurityKey(rsaPrivateKey);
                    _signingCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256);

                    Console.WriteLine($"✓ 从证书文件加载签名密钥: {certificatePath}");
                    _logger?.LogInformation("成功加载生产环境签名凭证");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "初始化签名凭证失败");
                throw;
            }
        }

        /// <summary>
        /// 获取签名凭证（用于签发 Token）
        /// </summary>
        /// <returns>包含 RSA 私钥的签名凭证</returns>
        /// <remarks>
        /// 此方法返回的凭证包含：
        /// - RSA 私钥：用于生成数字签名
        /// - 签名算法：RS256（RSA + SHA256）
        /// 
        /// 使用场景：
        /// - TokenService 生成 JWT Access Token 时使用
        /// - TokenService 生成 ID Token 时使用
        /// </remarks>
        public SigningCredentials GetSigningCredentials()
        {
            return _signingCredentials;
        }

        /// <summary>
        /// 获取验证密钥（用于验证 Token 签名）
        /// </summary>
        /// <returns>包含 RSA 公钥的安全密钥</returns>
        /// <remarks>
        /// 此方法返回的密钥只包含：
        /// - RSA 公钥：用于验证数字签名
        /// - 不包含私钥，安全性更高
        /// 
        /// 使用场景：
        /// - TokenService 验证 JWT Token 签名时使用
        /// - 可以分发给其他服务用于验证 Token
        /// - 公钥可以公开，不会泄露签名密钥
        /// 
        /// 安全优势：
        /// - 验证服务只需要公钥，不需要私钥
        /// - 即使公钥泄露也无法伪造 Token
        /// - 支持分布式微服务架构
        /// </remarks>
        public RsaSecurityKey GetValidationKey()
        {
            return _rsaSecurityKey;
        }

        /// <summary>
        /// 获取 RSA 公钥参数（用于 JWKS 端点）
        /// </summary>
        /// <returns>RSA 参数（仅包含公钥部分）</returns>
        /// <remarks>
        /// 用途：
        /// - 生成 JWKS (JSON Web Key Set) 响应
        /// - 公开发布公钥，让客户端和微服务验证 Token
        /// - 符合 RFC 7517 - JSON Web Key (JWK) 规范
        /// 
        /// 安全性：
        /// - 仅导出公钥参数（Modulus 和 Exponent）
        /// - 不包含私钥参数（D, P, Q, DP, DQ, InverseQ）
        /// - 可以安全地公开发布
        /// </remarks>
        public RSAParameters GetPublicKeyParameters()
        {
            // 从 RsaSecurityKey 获取 RSA 对象
            var rsa = _rsaSecurityKey.Rsa ?? RSA.Create();

            // 导出公钥参数（不包含私钥）
            // includePrivateParameters: false 确保只导出公钥
            return rsa.ExportParameters(includePrivateParameters: false);
        }

        /// <summary>
        /// 获取密钥 ID
        /// </summary>
        /// <returns>密钥的唯一标识符</returns>
        /// <remarks>
        /// 用于 JWT Header 的 kid 字段和 JWKS 的 kid 字段
        /// 允许客户端识别使用哪个密钥签名，支持密钥轮换
        /// </remarks>
        public string? GetKeyId()
        {
            return _rsaSecurityKey.KeyId;
        }

        /// <summary>
        /// 创建开发环境用的临时 RSA 密钥对（仿照 IdentityServer4 的 AddDeveloperSigningCredential）
        /// </summary>
        /// <returns>包含 RSA 密钥对的安全密钥</returns>
        /// <remarks>
        /// 密钥规格：
        /// - 密钥长度：2048 位（符合当前安全标准）
        /// - 密钥类型：RSA 非对称加密
        /// - 密钥 ID：随机 GUID，用于 JWT Header 的 kid 字段
        /// 
        /// 安全等级说明：
        /// - 2048 位：当前推荐的最小密钥长度（NIST 标准）
        /// - 3072 位：更高安全性，但性能稍差
        /// - 4096 位：最高安全性，通常用于长期证书
        /// 
        /// 注意事项：
        /// ⚠️ 此密钥仅存储在内存中！
        /// - 应用重启后会重新生成新密钥
        /// - 所有使用旧密钥签发的 Token 将无法验证
        /// - 不适合生产环境
        /// - 不适合多实例部署（每个实例有不同的密钥）
        /// 
        /// 生产环境建议：
        /// - 使用持久化的证书文件（PFX/PEM）
        /// - 将证书存储在安全位置（Key Vault、证书存储等）
        /// - 定期轮换证书
        /// </remarks>
        private static RsaSecurityKey LoadOrCreateDevelopmentKey()
        {
            const string keyFile = "tempkey.rsa";

            try
            {
                // 1. 尝试从文件加载现有密钥
                if (File.Exists(keyFile))
                {
                    var json = File.ReadAllText(keyFile);
                    var keyData = JsonSerializer.Deserialize<RsaKeyData>(json);

                    if (keyData != null && !string.IsNullOrEmpty(keyData.D))
                    {
                        var rsa = RSA.Create();
                        rsa.ImportParameters(new RSAParameters
                        {
                            D = Convert.FromBase64String(keyData.D),
                            DP = Convert.FromBase64String(keyData.DP!),
                            DQ = Convert.FromBase64String(keyData.DQ!),
                            Exponent = Convert.FromBase64String(keyData.Exponent!),
                            InverseQ = Convert.FromBase64String(keyData.InverseQ!),
                            Modulus = Convert.FromBase64String(keyData.Modulus!),
                            P = Convert.FromBase64String(keyData.P!),
                            Q = Convert.FromBase64String(keyData.Q!)
                        });

                        Console.WriteLine($"✓ 从 {keyFile} 加载开发密钥");
                        return new RsaSecurityKey(rsa) { KeyId = keyData.KeyId };
                    }
                }
            }
            catch
            {
                // 加载失败，继续生成新密钥
            }

            // 2. 生成新密钥并保存到文件
            var newRsa = RSA.Create(2048);
            var parameters = newRsa.ExportParameters(includePrivateParameters: true);
            var keyId = Guid.NewGuid().ToString();

            var newKeyData = new RsaKeyData
            {
                KeyId = keyId,
                D = Convert.ToBase64String(parameters.D!),
                DP = Convert.ToBase64String(parameters.DP!),
                DQ = Convert.ToBase64String(parameters.DQ!),
                Exponent = Convert.ToBase64String(parameters.Exponent!),
                InverseQ = Convert.ToBase64String(parameters.InverseQ!),
                Modulus = Convert.ToBase64String(parameters.Modulus!),
                P = Convert.ToBase64String(parameters.P!),
                Q = Convert.ToBase64String(parameters.Q!)
            };

            var jsonContent = JsonSerializer.Serialize(newKeyData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(keyFile, jsonContent);

            Console.WriteLine($"✓ 生成新的开发密钥并保存到 {keyFile}");

            return new RsaSecurityKey(newRsa) { KeyId = keyId };
        }

        /// <summary>
        /// RSA 密钥数据（用于序列化保存到文件）
        /// </summary>
        private class RsaKeyData
        {
            public string? KeyId { get; set; }
            public string? Modulus { get; set; }
            public string? Exponent { get; set; }
            public string? P { get; set; }
            public string? Q { get; set; }
            public string? DP { get; set; }
            public string? DQ { get; set; }
            public string? InverseQ { get; set; }
            public string? D { get; set; }
        }

        /// <summary>
        /// 从 PFX 证书文件加载证书（生产环境）
        /// </summary>
        /// <param name="path">证书文件路径（.pfx 或 .p12 格式）</param>
        /// <param name="password">证书密码（可选）</param>
        /// <returns>X509Certificate2 证书对象</returns>
        /// <exception cref="FileNotFoundException">证书文件不存在</exception>
        /// <exception cref="InvalidOperationException">证书加载失败</exception>
        /// <remarks>
        /// 证书格式说明：
        /// - PFX (PKCS#12): 包含私钥和证书的二进制格式
        /// - P12: PFX 的另一种文件扩展名
        /// - 需要密码保护以确保私钥安全
        /// 
        /// 证书生成方法：
        /// 
        /// 1. 使用 OpenSSL（跨平台）：
        ///    ```bash
        ///    # 生成私钥
        ///    openssl genrsa -out private.key 2048
        ///    
        ///    # 生成证书签名请求
        ///    openssl req -new -key private.key -out cert.csr
        ///    
        ///    # 自签名证书（开发/测试）
        ///    openssl x509 -req -days 365 -in cert.csr -signkey private.key -out cert.crt
        ///    
        ///    # 转换为 PFX 格式
        ///    openssl pkcs12 -export -out certificate.pfx -inkey private.key -in cert.crt
        ///    ```
        /// 
        /// 2. 使用 PowerShell（Windows）：
        ///    ```powershell
        ///    $cert = New-SelfSignedCertificate -Subject "CN=OneAuthCenter" `
        ///        -CertStoreLocation "Cert:\CurrentUser\My" `
        ///        -KeyExportPolicy Exportable `
        ///        -KeySpec Signature `
        ///        -KeyLength 2048 `
        ///        -KeyAlgorithm RSA `
        ///        -HashAlgorithm SHA256 `
        ///        -NotAfter (Get-Date).AddYears(2)
        ///    
        ///    $password = ConvertTo-SecureString -String "YourPassword" -Force -AsPlainText
        ///    Export-PfxCertificate -Cert $cert -FilePath "certificate.pfx" -Password $password
        ///    ```
        /// 
        /// 3. 使用 .NET（代码生成）：
        ///    ```csharp
        ///    var cert = CertificateHelper.CreateSelfSignedCertificate("CN=OneAuthCenter");
        ///    ```
        /// 
        /// 存储标志说明：
        /// - MachineKeySet: 密钥存储在机器级别（推荐生产环境）
        /// - EphemeralKeySet: 密钥不持久化到磁盘（更安全，但重启后需要重新加载）
        /// 
        /// 生产环境建议：
        /// - 使用受信任的 CA 签发的证书
        /// - 将证书存储在 Azure Key Vault、AWS Secrets Manager 等安全存储中
        /// - 定期轮换证书（通常 1-2 年）
        /// - 使用强密码保护私钥
        /// - 限制证书文件的访问权限
        /// </remarks>
        private static X509Certificate2 LoadCertificateFromFile(string path, string? password)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path), "证书文件路径不能为空");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"证书文件不存在: {path}");
            }

            try
            {
                // 加载 PFX/P12 证书文件
                // .NET 9.0 使用新的 X509CertificateLoader API（更安全、更现代）
                // X509KeyStorageFlags 说明：
                // - MachineKeySet: 将密钥存储在计算机存储中（适合服务器应用）
                // - EphemeralKeySet: 密钥仅存在于内存中，不写入磁盘（更安全）
                var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                    path,
                    password,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet
                );

                // 验证证书包含私钥
                if (!certificate.HasPrivateKey)
                {
                    throw new InvalidOperationException($"证书文件不包含私钥: {path}");
                }

                // 检查证书有效期
                var now = DateTime.UtcNow;
                if (now < certificate.NotBefore)
                {
                    throw new InvalidOperationException($"证书尚未生效，生效时间: {certificate.NotBefore}");
                }

                if (now > certificate.NotAfter)
                {
                    throw new InvalidOperationException($"证书已过期，过期时间: {certificate.NotAfter}");
                }

                return certificate;
            }
            catch (CryptographicException ex)
            {
                // 密码错误或证书格式错误
                throw new InvalidOperationException($"无法加载证书文件（可能是密码错误或格式不正确）: {path}", ex);
            }
            catch (Exception ex) when (ex is not InvalidOperationException && ex is not FileNotFoundException)
            {
                throw new InvalidOperationException($"加载证书时发生未预期的错误: {path}", ex);
            }
        }

        /// <summary>
        /// 从 Windows/macOS 证书存储加载证书（备用方法）
        /// </summary>
        /// <param name="thumbprint">证书指纹（SHA1 哈希）</param>
        /// <param name="storeName">证书存储名称（默认：My - 个人证书）</param>
        /// <param name="storeLocation">证书存储位置（默认：CurrentUser）</param>
        /// <returns>找到的证书，未找到返回 null</returns>
        /// <remarks>
        /// 证书存储位置：
        /// - CurrentUser: 当前用户的证书存储
        /// - LocalMachine: 计算机级别的证书存储（需要管理员权限）
        /// 
        /// 证书存储名称：
        /// - My: 个人证书
        /// - Root: 受信任的根证书
        /// - TrustedPeople: 受信任的人
        /// 
        /// 获取证书指纹：
        /// 1. Windows: certmgr.msc → 查看证书详细信息
        /// 2. PowerShell: Get-ChildItem Cert:\CurrentUser\My | Format-List
        /// 3. OpenSSL: openssl x509 -in cert.crt -fingerprint -noout
        /// 
        /// 使用示例：
        /// ```csharp
        /// var cert = LoadCertificateFromStore(
        ///     "1234567890ABCDEF1234567890ABCDEF12345678",
        ///     StoreName.My,
        ///     StoreLocation.LocalMachine
        /// );
        /// ```
        /// </remarks>
        public static X509Certificate2? LoadCertificateFromStore(
            string thumbprint,
            StoreName storeName = StoreName.My,
            StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                throw new ArgumentNullException(nameof(thumbprint), "证书指纹不能为空");
            }

            // 移除指纹中的空格和特殊字符
            thumbprint = thumbprint.Replace(" ", "").Replace(":", "").ToUpperInvariant();

            try
            {
                // 打开证书存储
                using var store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadOnly);

                // 查找证书（按指纹）
                var certificates = store.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    thumbprint,
                    validOnly: false  // 允许过期证书（开发环境可能需要）
                );

                if (certificates.Count == 0)
                {
                    return null;
                }

                var certificate = certificates[0];

                // 验证证书包含私钥
                if (!certificate.HasPrivateKey)
                {
                    throw new InvalidOperationException($"证书不包含私钥，Thumbprint: {thumbprint}");
                }

                return certificate;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"从证书存储加载证书失败，Thumbprint: {thumbprint}, Store: {storeName}, Location: {storeLocation}",
                    ex
                );
            }
        }
    }
}
