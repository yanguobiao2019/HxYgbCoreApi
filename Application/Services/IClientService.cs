using Application.DTOs.Client;

namespace Application.Services;

/// <summary>
/// 客户端服务接口
/// </summary>
public interface IClientService
{
    /// <summary>
    /// 获取客户端信息
    /// </summary>
    Task<ClientDto?> GetClientAsync(string clientId);
    
    /// <summary>
    /// 获取所有客户端
    /// </summary>
    Task<IEnumerable<ClientDto>> GetAllClientsAsync();
    
    /// <summary>
    /// 创建客户端
    /// </summary>
    Task<ClientDto> CreateClientAsync(CreateClientRequest request);
    
    /// <summary>
    /// 更新客户端
    /// </summary>
    Task UpdateClientAsync(string clientId, CreateClientRequest request);
    
    /// <summary>
    /// 删除客户端
    /// </summary>
    Task DeleteClientAsync(string clientId);
    
    /// <summary>
    /// 验证客户端凭证
    /// </summary>
    Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret);
    
    /// <summary>
    /// 重置客户端密钥
    /// </summary>
    Task<string> ResetClientSecretAsync(string clientId);
}

