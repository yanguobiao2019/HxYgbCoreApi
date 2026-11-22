using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 客户端仓储实现
/// </summary>
public class ClientRepository : IClientRepository
{
    private readonly AuthDbContext _context;

    public ClientRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetByIdAsync(int id)
    {
        return await _context.Clients.FindAsync(id);
    }

    public async Task<Client?> GetByClientIdAsync(string clientId)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.ClientId == clientId);
    }

    public async Task<IEnumerable<Client>> GetAllAsync()
    {
        return await _context.Clients.ToListAsync();
    }

    public async Task<Client> CreateAsync(Client client)
    {
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task UpdateAsync(Client client)
    {
        _context.Clients.Update(client);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var client = await GetByIdAsync(id);
        if (client != null)
        {
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ValidateClientAsync(string clientId, string clientSecret)
    {
        var client = await GetByClientIdAsync(clientId);
        return client != null && client.ClientSecret == clientSecret && client.IsActive;
    }
}

