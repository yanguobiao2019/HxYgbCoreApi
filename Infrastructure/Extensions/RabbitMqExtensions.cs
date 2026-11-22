using Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Extensions
{
    public static class RabbitMqExtensions
    {
        public static IServiceCollection AddRabbitMqConfigure(this IServiceCollection services, IConfiguration configuration) {
            var rabbitMqSettings = configuration.GetSection("RabbitMq");
            // 显式检查必需配置项，缺失时抛出可诊断的异常
            var hostName = rabbitMqSettings["HostName"] ?? throw new InvalidOperationException("Missing configuration: RabbitMq:HostName");
            var userName = rabbitMqSettings["UserName"] ?? throw new InvalidOperationException("Missing configuration: RabbitMq:UserName");
            var password = rabbitMqSettings["Password"] ?? throw new InvalidOperationException("Missing configuration: RabbitMq:Password");
            var port = rabbitMqSettings.GetValue<int>("Port");
            
            services.AddSingleton<RabbitMqConfig>(new RabbitMqConfig
            {
                HostName = hostName,
                Port = port.ToString(),
                UserName = userName,
                Password = password
            });
            return services;
        }
    }
}
