using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// 用户同意记录仓储接口
/// </summary>
public interface IUserConsentRepository
{
    Task<UserConsent?> GetByUserAndClientAsync(int userId, int clientId);
    Task<IEnumerable<UserConsent>> GetByUserIdAsync(int userId);
    Task<UserConsent> CreateAsync(UserConsent consent);
    Task UpdateAsync(UserConsent consent);
    Task DeleteAsync(int id);
    Task DeleteByUserAndClientAsync(int userId, int clientId);
}

