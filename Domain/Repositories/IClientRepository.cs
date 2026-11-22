using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// 客户端仓储接口
/// </summary>
public interface IClientRepository
{
    Task<Client?> GetByIdAsync(int id);
    Task<Client?> GetByClientIdAsync(string clientId);
    Task<IEnumerable<Client>> GetAllAsync();
    Task<Client> CreateAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(int id);
    Task<bool> ValidateClientAsync(string clientId, string clientSecret);
}

