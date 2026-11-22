using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Infrastructure.Extensions
{
    public static class SerilogExtensions
    {
        public static IServiceCollection AddSerilogConfigure(this IServiceCollection services,IConfiguration configuration)
        {
            string connectionString = configuration["SerilogSettings:connectionString"] ?? "";
            var columnOptions = new ColumnOptions()
            {
                TimeStamp = { ConvertToUtc = true },
                AdditionalColumns = new List<SqlColumn> // 自定义列，存储当前机器名
                {
                    new SqlColumn
                    {
                        DataType = SqlDbType.NVarChar,
                        DataLength = 255, // 对于字符串类型，建议指定长度
                        ColumnName = "MachineName"
                    },
                    new SqlColumn
                    {
                        DataType = SqlDbType.NVarChar,
                        DataLength = 100,
                        ColumnName = "UserName", // 新增一个动态列：用户名
                        AllowNull = true
                    },
                    new SqlColumn
                    {
                        DataType = SqlDbType.NVarChar,
                        DataLength = 50,
                        ColumnName = "RequestId", // 新增一个动态列：请求 ID
                        AllowNull = true
                    }
                }
            };
            //columnOptions.Store.Remove(StandardColumn.MessageTemplate); // 移除 MessageTemplate 列
            //columnOptions.Store.Remove(StandardColumn.Properties); 
            columnOptions.Properties.DataType = SqlDbType.NVarChar;
            Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Warning()
                            //.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                            .Enrich.FromLogContext()
                            //全部日志记录都带上参数：MachineName,注意：.Enrich.WithProperty(...) 是一个一次性 enricher，适用于静态值
                            //如果需要动态或更复杂的 enricher，可以实现 ILogEventEnricher
                            .Enrich.WithProperty("MachineName", Environment.MachineName) 
                            .WriteTo.Console() // 可选：同时输出到控制台
                            .WriteTo.Async(a => a.MSSqlServer(
                                           connectionString: connectionString, //?? "",
                                            sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                                            {
                                                TableName = "Logs",
                                                AutoCreateSqlTable = true, // 自动建表（首次运行时）
                                                //BatchPostingLimit = 100, // 每 100 条提交一次
                                                //BatchPeriod = TimeSpan.FromSeconds(90)
                                            },
                                           columnOptions: columnOptions)
                            )
                         .CreateLogger();
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });
            return services;
        }
    }
}
//var columnOptions = new ColumnOptions() {
//    TimeStamp = { ConvertToUtc = true },
//    AdditionalColumns = new List<SqlColumn> // 自定义列，存储当前机器名
//            {
//                new SqlColumn
//                {
//                    DataType = SqlDbType.NVarChar,
//                    DataLength = 255, // 对于字符串类型，建议指定长度
//                    ColumnName = "MachineName"
//                },
//                new SqlColumn
//                {
//                    DataType = SqlDbType.NVarChar,
//                    DataLength = 100,
//                    ColumnName = "UserName", // 新增一个动态列：用户名
//                    AllowNull = true
//                },
//                new SqlColumn
//                {
//                    DataType = SqlDbType.NVarChar,
//                    DataLength = 50,
//                    ColumnName = "RequestId", // 新增一个动态列：请求 ID
//                    AllowNull = true
//                }
//            }
//};
////columnOptions.Store.Remove(StandardColumn.MessageTemplate); // 移除 MessageTemplate 列
////columnOptions.Store.Remove(StandardColumn.Properties); 
//columnOptions.Properties.DataType = SqlDbType.NVarChar;
//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Warning()
//    //.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
//    .Enrich.FromLogContext()
//    .Enrich.WithProperty("MachineName", Environment.MachineName) // 全部日志记录都带上参数：MachineName
//                                                                 // 注意：.Enrich.WithProperty(...) 是一个一次性 enricher，适用于静态值
//                                                                 // 如果需要动态或更复杂的 enricher，可以实现 ILogEventEnricher
//    .WriteTo.Console() // 可选：同时输出到控制台
//    .WriteTo.Async(a=>a.MSSqlServer(
//        //          "SqlServer": ".;Database=HxYgbDb;UID‌=sa,PWD=850415,Trusted_Connection=True;MultipleActiveResultSets=true",
//        connectionString: "Server=localhost;Database=HxYgbDb;Trusted_Connection=True;TrustServerCertificate=True;",
//        sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
//        {
//            TableName = "Logs",
//            AutoCreateSqlTable = true, // 自动建表（首次运行时）
//            //BatchPostingLimit = 100, // 每 100 条提交一次
//            //BatchPeriod = TimeSpan.FromSeconds(90)
//        },
//       columnOptions: columnOptions)
//    )
//    .CreateLogger();

//builder.Services.AddLogging(loggingBuilder =>
//{
//    loggingBuilder.ClearProviders();
//    loggingBuilder.AddSerilog(dispose: true);
//});