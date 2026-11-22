using System.Security.Cryptography;
using Application.DTOs.Client;
using Domain.Entities;
using Domain.Repositories;

namespace Application.Services.Implementations;

/// <summary>
/// 客户端服务实现
/// </summary>
public class ClientService : IClientService
{
    private readonly IClientRepository _clientRepository;

    public ClientService(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<ClientDto?> GetClientAsync(string clientId)
    {
        var client = await _clientRepository.GetByClientIdAsync(clientId);
        return client == null ? null : MapToDto(client);
    }

    public async Task<IEnumerable<ClientDto>> GetAllClientsAsync()
    {
        var clients = await _clientRepository.GetAllAsync();
        return clients.Select(MapToDto);
    }

    public async Task<ClientDto> CreateClientAsync(CreateClientRequest request)
    {
        // 检查客户端 ID 是否已存在
        var existing = await _clientRepository.GetByClientIdAsync(request.ClientId);
        if (existing != null)
        {
            throw new InvalidOperationException($"客户端 ID '{request.ClientId}' 已存在");
        }

        var client = new Client
        {
            ClientId = request.ClientId,
            ClientSecret = GenerateClientSecret(),
            ClientName = request.ClientName,
            Description = request.Description,
            RedirectUris = string.Join(",", request.RedirectUris),
            PostLogoutRedirectUris = string.Join(",", request.PostLogoutRedirectUris),
            AllowedGrantTypes = string.Join(",", request.AllowedGrantTypes),
            AllowedScopes = string.Join(",", request.AllowedScopes),
            AccessTokenLifetime = request.AccessTokenLifetime,
            RefreshTokenLifetime = request.RefreshTokenLifetime,
            RequireConsent = request.RequireConsent,
            RequirePkce = request.RequirePkce,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdClient = await _clientRepository.CreateAsync(client);
        return MapToDto(createdClient);
    }

    public async Task UpdateClientAsync(string clientId, CreateClientRequest request)
    {
        var client = await _clientRepository.GetByClientIdAsync(clientId);
        if (client == null)
        {
            throw new KeyNotFoundException($"客户端 '{clientId}' 不存在");
        }

        client.ClientName = request.ClientName;
        client.Description = request.Description;
        client.RedirectUris = string.Join(",", request.RedirectUris);
        client.PostLogoutRedirectUris = string.Join(",", request.PostLogoutRedirectUris);
        client.AllowedGrantTypes = string.Join(",", request.AllowedGrantTypes);
        client.AllowedScopes = string.Join(",", request.AllowedScopes);
        client.AccessTokenLifetime = request.AccessTokenLifetime;
        client.RefreshTokenLifetime = request.RefreshTokenLifetime;
        client.RequireConsent = request.RequireConsent;
        client.RequirePkce = request.RequirePkce;

        await _clientRepository.UpdateAsync(client);
    }

    public async Task DeleteClientAsync(string clientId)
    {
        var client = await _clientRepository.GetByClientIdAsync(clientId);
        if (client == null)
        {
            throw new KeyNotFoundException($"客户端 '{clientId}' 不存在");
        }

        await _clientRepository.DeleteAsync(client.Id);
    }

    public async Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret)
    {
        return await _clientRepository.ValidateClientAsync(clientId, clientSecret);
    }

    public async Task<string> ResetClientSecretAsync(string clientId)
    {
        var client = await _clientRepository.GetByClientIdAsync(clientId);
        if (client == null)
        {
            throw new KeyNotFoundException($"客户端 '{clientId}' 不存在");
        }

        var newSecret = GenerateClientSecret();
        client.ClientSecret = newSecret;
        await _clientRepository.UpdateAsync(client);

        return newSecret;
    }

    private ClientDto MapToDto(Client client)
    {
        return new ClientDto
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            Description = client.Description,
            RedirectUris = client.RedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries),
            PostLogoutRedirectUris = client.PostLogoutRedirectUris?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(),
            AllowedGrantTypes = client.AllowedGrantTypes.Split(',', StringSplitOptions.RemoveEmptyEntries),
            AllowedScopes = client.AllowedScopes.Split(',', StringSplitOptions.RemoveEmptyEntries),
            AccessTokenLifetime = client.AccessTokenLifetime,
            RefreshTokenLifetime = client.RefreshTokenLifetime,
            RequireConsent = client.RequireConsent,
            RequirePkce = client.RequirePkce,
            IsActive = client.IsActive,
            CreatedAt = client.CreatedAt
        };
    }

    private string GenerateClientSecret()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

