using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// 授权码仓储接口
/// </summary>
public interface IAuthorizationCodeRepository
{
    Task<AuthorizationCode?> GetByCodeAsync(string code);
    Task<AuthorizationCode> CreateAsync(AuthorizationCode authorizationCode);
    Task UpdateAsync(AuthorizationCode authorizationCode);
    Task DeleteAsync(int id);
    Task DeleteExpiredAsync();
}

