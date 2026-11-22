using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 用户同意记录仓储实现
/// </summary>
public class UserConsentRepository : IUserConsentRepository
{
    private readonly AuthDbContext _context;

    public UserConsentRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<UserConsent?> GetByUserAndClientAsync(int userId, int clientId)
    {
        return await _context.UserConsents
            .Include(uc => uc.User)
            .Include(uc => uc.Client)
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ClientId == clientId);
    }

    public async Task<IEnumerable<UserConsent>> GetByUserIdAsync(int userId)
    {
        return await _context.UserConsents
            .Include(uc => uc.Client)
            .Where(uc => uc.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserConsent> CreateAsync(UserConsent consent)
    {
        _context.UserConsents.Add(consent);
        await _context.SaveChangesAsync();
        return consent;
    }

    public async Task UpdateAsync(UserConsent consent)
    {
        _context.UserConsents.Update(consent);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var consent = await _context.UserConsents.FindAsync(id);
        if (consent != null)
        {
            _context.UserConsents.Remove(consent);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByUserAndClientAsync(int userId, int clientId)
    {
        var consent = await GetByUserAndClientAsync(userId, clientId);
        if (consent != null)
        {
            _context.UserConsents.Remove(consent);
            await _context.SaveChangesAsync();
        }
    }
}

