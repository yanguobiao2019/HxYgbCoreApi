namespace Infrastructure.Configuration;

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
    
    /// <summary>
    /// 连接字符串配置
    /// </summary>
    public ConnectionStrings ConnectionStrings { get; set; } = new();
}

