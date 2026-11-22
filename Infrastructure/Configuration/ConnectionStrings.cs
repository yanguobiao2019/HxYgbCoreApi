namespace Infrastructure.Configuration;

/// <summary>
/// 连接字符串配置
/// </summary>
public class ConnectionStrings
{
    public string SqlServer { get; set; } = string.Empty;
    public string MySql { get; set; } = string.Empty;
    public string PostgreSql { get; set; } = string.Empty;
    public string Sqlite { get; set; } = string.Empty;
    public string Oracle { get; set; } = string.Empty;
}

