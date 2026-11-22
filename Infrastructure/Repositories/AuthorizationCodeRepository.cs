using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories;

/// <summary>
/// 授权码仓储实现
/// </summary>
public class AuthorizationCodeRepository : IAuthorizationCodeRepository
{
    private readonly AuthDbContext _context;

    public AuthorizationCodeRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorizationCode?> GetByCodeAsync(string code)
    {
        return await _context.AuthorizationCodes
            .Include(ac => ac.Client)
            .Include(ac => ac.User)
            .FirstOrDefaultAsync(ac => ac.Code == code);
    }

    public async Task<AuthorizationCode> CreateAsync(AuthorizationCode authorizationCode)
    {
        _context.AuthorizationCodes.Add(authorizationCode);
        await _context.SaveChangesAsync();
        return authorizationCode;
    }

    public async Task UpdateAsync(AuthorizationCode authorizationCode)
    {
        _context.AuthorizationCodes.Update(authorizationCode);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var authCode = await _context.AuthorizationCodes.FindAsync(id);
        if (authCode != null)
        {
            _context.AuthorizationCodes.Remove(authCode);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteExpiredAsync()
    {
        var expiredCodes = await _context.AuthorizationCodes
            .Where(ac => ac.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.AuthorizationCodes.RemoveRange(expiredCodes);
        await _context.SaveChangesAsync();
    }
}

