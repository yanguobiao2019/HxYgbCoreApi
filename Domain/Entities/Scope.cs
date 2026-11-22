namespace Domain.Entities;

/// <summary>
/// OAuth 2.0 作用域实体
/// </summary>
public class Scope
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string DisplayName { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    /// <summary>
    /// 是否必需（用户无法取消勾选）
    /// </summary>
    public bool Required { get; set; } = false;
    
    /// <summary>
    /// 是否强调显示（在同意页面中突出显示）
    /// </summary>
    public bool Emphasize { get; set; } = false;
    
    /// <summary>
    /// 是否在发现文档中显示
    /// </summary>
    public bool ShowInDiscoveryDocument { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

