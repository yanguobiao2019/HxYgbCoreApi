using Infrastructure.Configuration;
using Infrastructure.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Extensions
{
    public static class AIServiceColletctionExtensions
    {
        public static IServiceCollection AddAIServiceColletction(this IServiceCollection services, IConfiguration configuration)
        {
            var AiSettings = configuration.GetSection("AI");
            // 显式检查必需配置项，缺失时抛出可诊断的异常
            var endpoint = AiSettings["Endpoint"] ?? throw new InvalidOperationException("AI:Endpoint 配置缺失");
            var apiKey = AiSettings["ApiKey"] ?? throw new InvalidOperationException("Missing configuration: ApiKey");
            var modelId = AiSettings["ModelId"] ?? throw new InvalidOperationException("Missing configuration: AI:ModelId");

            // 检查 API Key
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  警告: AI:ApiKey 未配置，请在 appsettings.json 或环境变量中设置");
                Console.WriteLine("   您可以使用以下方式配置:");
                Console.WriteLine("   1. 在 appsettings.json 中设置 AI:ApiKey");
                Console.WriteLine("   2. 设置环境变量: $env:AI__ApiKey=\"your-api-key\"");
                Console.WriteLine("   3. 使用 .NET User Secrets");
                Console.ResetColor();

                // 使用占位符，实际运行时会失败
                apiKey = "your-api-key-here";
            }
            // 获取性能配置
            var enableMonitoring = configuration.GetValue<bool>("Performance:EnableMonitoring", true);
            var enableLogging = configuration.GetValue<bool>("Performance:EnableLogging", true);
            var enableFilter = configuration.GetValue<bool>("Performance:EnableSensitiveWordFilter", true);
            OpenAIClientOptions clientOptions = new OpenAIClientOptions();
            clientOptions.Endpoint = new Uri(endpoint);
            OpenAIClient aiClient = new(new ApiKeyCredential(apiKey), clientOptions);
            IChatClient chatClient = aiClient.GetChatClient(modelId).AsIChatClient();
            // 使用 ChatClientBuilder 构建中间件管道
            var builder = new ChatClientBuilder(chatClient);

            // 1. 缓存中间件（如果启用）
            if (configuration.GetValue<bool>("AI:EnableCaching", true))
            {
                var cache = services.BuildServiceProvider().GetRequiredService<IMemoryCache>();
                var distributedCache = new MemoryCacheAdapter(cache);
                builder.UseDistributedCache(distributedCache);
            }

            // 2. Function Calling 中间件
            builder.UseFunctionInvocation();

            // 构建带中间件的客户端
            var clientWithMiddleware = builder.Build();

            // 3. 手动包装自定义中间件（因为它们需要 IServiceProvider）
            services.AddSingleton<IChatClient>(sp =>
            {
                IChatClient client = clientWithMiddleware;

                // 敏感词过滤中间件（如果启用）
                if (enableFilter)
                {
                    var logger = sp.GetRequiredService<ILogger<SensitiveWordFilterChatClient>>();
                    client = new SensitiveWordFilterChatClient(client, logger);
                }

                // 性能监控中间件（如果启用）
                if (enableMonitoring)
                {
                    var logger = sp.GetRequiredService<ILogger<PerformanceMonitorChatClient>>();
                    client = new PerformanceMonitorChatClient(client, logger);
                }

                // 日志中间件（最外层，如果启用）
                if (enableLogging)
                {
                    var logger = sp.GetRequiredService<ILogger<Utility.LoggingChatClient>>();
                    client = new Utility.LoggingChatClient(client, logger);
                }

                return client;
            });
            return services;
        }
        /// <summary>
        /// 简单的内存缓存适配器（演示用）
        /// </summary>
        private class MemoryCacheAdapter : Microsoft.Extensions.Caching.Distributed.IDistributedCache
        {
            private readonly IMemoryCache _memoryCache;

            public MemoryCacheAdapter(IMemoryCache memoryCache)
            {
                _memoryCache = memoryCache;
            }

            public byte[]? Get(string key)
            {
                return _memoryCache.Get<byte[]>(key);
            }

            public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            {
                return Task.FromResult(_memoryCache.Get<byte[]>(key));
            }

            public void Set(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options)
            {
                var memoryCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
                    SlidingExpiration = options.SlidingExpiration
                };

                _memoryCache.Set(key, value, memoryCacheOptions);
            }

            public Task SetAsync(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, CancellationToken token = default)
            {
                Set(key, value, options);
                return Task.CompletedTask;
            }

            public void Refresh(string key)
            {
                // Memory cache doesn't need explicit refresh
            }

            public Task RefreshAsync(string key, CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public void Remove(string key)
            {
                _memoryCache.Remove(key);
            }

            public Task RemoveAsync(string key, CancellationToken token = default)
            {
                _memoryCache.Remove(key);
                return Task.CompletedTask;
            }
        }
    }
}
