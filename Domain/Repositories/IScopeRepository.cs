using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// 作用域仓储接口
/// </summary>
public interface IScopeRepository
{
    Task<Scope?> GetByIdAsync(int id);
    Task<Scope?> GetByNameAsync(string name);
    Task<IEnumerable<Scope>> GetAllAsync();
    Task<IEnumerable<Scope>> GetByNamesAsync(IEnumerable<string> names);
    Task<Scope> CreateAsync(Scope scope);
    Task UpdateAsync(Scope scope);
    Task DeleteAsync(int id);
}

