namespace Application.DTOs.Client;

/// <summary>
/// 客户端 DTO
/// </summary>
public class ClientDto
{
    public int Id { get; set; }
    
    public string ClientId { get; set; } = string.Empty;
    
    public string ClientName { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string[] RedirectUris { get; set; } = Array.Empty<string>();
    
    public string[] PostLogoutRedirectUris { get; set; } = Array.Empty<string>();
    
    public string[] AllowedGrantTypes { get; set; } = Array.Empty<string>();
    
    public string[] AllowedScopes { get; set; } = Array.Empty<string>();
    
    public int AccessTokenLifetime { get; set; }
    
    public int RefreshTokenLifetime { get; set; }
    
    public bool RequireConsent { get; set; }
    
    public bool RequirePkce { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

