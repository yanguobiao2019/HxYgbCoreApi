using Infrastructure.Configuration;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 根据配置添加数据库上下文
        /// </summary>
        public static IServiceCollection AddConfiguredDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 读取数据库配置
            var databaseSettings = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>()
                ?? throw new InvalidOperationException("数据库配置未找到");

            // 获取迁移程序集名称
            var migrationsAssembly = typeof(AuthDbContext).Assembly.GetName().Name;

            // 根据配置的数据库类型注入对应的数据库上下文
            switch (databaseSettings.DatabaseType)
            {
                case DatabaseType.SqlServer:
                    services.AddDbContext<AuthDbContext>(options =>
                    {
                        options.UseSqlServer(
                            databaseSettings.ConnectionStrings.SqlServer,
                            b => b.MigrationsAssembly(migrationsAssembly));
                    });
                    Console.WriteLine("✓ 使用 SQL Server 数据库");
                    break;

                //case DatabaseType.MySql:
                //    services.AddDbContext<AuthDbContext>(options =>
                //    {
                //        var serverVersion = ServerVersion.AutoDetect(databaseSettings.ConnectionStrings.MySql);
                //        options.UseMySql(
                //            databaseSettings.ConnectionStrings.MySql,
                //            serverVersion,
                //            b => b.MigrationsAssembly(migrationsAssembly));
                //    });
                //    Console.WriteLine("✓ 使用 MySQL 数据库");
                //    break;

                //case DatabaseType.PostgreSql:
                //    services.AddDbContext<AuthDbContext>(options =>
                //    {
                //        options.UseNpgsql(
                //            databaseSettings.ConnectionStrings.PostgreSql,
                //            b => b.MigrationsAssembly(migrationsAssembly));
                //    });
                //    Console.WriteLine("✓ 使用 PostgreSQL 数据库");
                //    break;

                //case DatabaseType.Sqlite:
                //    services.AddDbContext<AuthDbContext>(options =>
                //    {
                //        options.UseSqlite(
                //            databaseSettings.ConnectionStrings.Sqlite,
                //            b => b.MigrationsAssembly(migrationsAssembly));
                //    });
                //    Console.WriteLine("✓ 使用 SQLite 数据库");
                //    break;

                //case DatabaseType.Oracle:
                //    services.AddDbContext<AuthDbContext>(options =>
                //    {
                //        options.UseOracle(
                //            databaseSettings.ConnectionStrings.Oracle,
                //            b => b.MigrationsAssembly(migrationsAssembly));
                //    });
                //    Console.WriteLine("✓ 使用 Oracle 数据库");
                //    break;

                default:
                    throw new NotSupportedException($"不支持的数据库类型: {databaseSettings.DatabaseType}");
            }

            return services;
        }

        /// <summary>
        /// 获取当前配置的数据库类型
        /// </summary>
        public static DatabaseType GetConfiguredDatabaseType(this IConfiguration configuration)
        {
            var databaseSettings = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>();
            return databaseSettings?.DatabaseType ?? DatabaseType.SqlServer;
        }

        /// <summary>
        /// 获取当前配置的连接字符串
        /// </summary>
        public static string GetConfiguredConnectionString(this IConfiguration configuration)
        {
            var databaseSettings = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>()
                ?? throw new InvalidOperationException("数据库配置未找到");

            return databaseSettings.DatabaseType switch
            {
                DatabaseType.SqlServer => databaseSettings.ConnectionStrings.SqlServer,
                DatabaseType.MySql => databaseSettings.ConnectionStrings.MySql,
                DatabaseType.PostgreSql => databaseSettings.ConnectionStrings.PostgreSql,
                DatabaseType.Sqlite => databaseSettings.ConnectionStrings.Sqlite,
                DatabaseType.Oracle => databaseSettings.ConnectionStrings.Oracle,
                _ => throw new NotSupportedException($"不支持的数据库类型: {databaseSettings.DatabaseType}")
            };
        }
    }
}
