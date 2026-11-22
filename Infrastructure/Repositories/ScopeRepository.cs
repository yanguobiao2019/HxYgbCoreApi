using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 作用域仓储实现
/// </summary>
public class ScopeRepository : IScopeRepository
{
    private readonly AuthDbContext _context;

    public ScopeRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<Scope?> GetByIdAsync(int id)
    {
        return await _context.Scopes.FindAsync(id);
    }

    public async Task<Scope?> GetByNameAsync(string name)
    {
        return await _context.Scopes
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<IEnumerable<Scope>> GetAllAsync()
    {
        return await _context.Scopes.ToListAsync();
    }

    public async Task<IEnumerable<Scope>> GetByNamesAsync(IEnumerable<string> names)
    {
        return await _context.Scopes
            .Where(s => names.Contains(s.Name))
            .ToListAsync();
    }

    public async Task<Scope> CreateAsync(Scope scope)
    {
        _context.Scopes.Add(scope);
        await _context.SaveChangesAsync();
        return scope;
    }

    public async Task UpdateAsync(Scope scope)
    {
        _context.Scopes.Update(scope);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var scope = await GetByIdAsync(id);
        if (scope != null)
        {
            _context.Scopes.Remove(scope);
            await _context.SaveChangesAsync();
        }
    }
}

