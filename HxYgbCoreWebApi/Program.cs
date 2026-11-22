using Application.Services;
using Application.Services.Implementations;
using Domain.Repositories;
using HxYgbCoreWebApi.Filters;
using HxYgbCoreWebApi.Middleware;
using HxYgbCoreWebApi.Utility;
using Infrastructure.AI;
using Infrastructure.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Redis;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Metrics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ScaffoldData.Context;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using ServiceStack;
using System.Configuration;
using System.Data;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options => { 
    options.Filters.Add<AppResultFilter>();
});
builder.Services.AddEndpointsApiExplorer();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "Fixed", options =>
    {
        options.PermitLimit = 2; // 每个时间窗口允许的请求数
        options.Window = TimeSpan.FromSeconds(20); // 时间窗口长度
        options.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst; // 排队处理顺序
        options.QueueLimit = 1; // 队列中允许的最大请求数
    });
    options.OnRejected = (context, cancellationToken) => {
        Console.WriteLine("OnRejected");
        return ValueTask.CompletedTask;
    };
});
Metric.Config
    .WithHttpEndpoint("http://localhost:1234/")
    .WithAllCounters()
    .WithReporting((rc) =>
    {
        // 报表输出到控制台
        rc.WithConsoleReport(TimeSpan.FromSeconds(5));
    });
// 配置 Swagger（从配置文件读取）
var swaggerSettings = builder.Configuration.GetSection("SwaggerSettings");
if (swaggerSettings.GetValue<bool>("Enabled", true)) {
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = swaggerSettings["Title"] ?? "OneAuthCenter API",
            Version = swaggerSettings["Version"] ?? "v1",
            Description = swaggerSettings["Description"] ?? "OneAuthCenter 认证中心",
            Contact = new OpenApiContact
            {
                Name = swaggerSettings["ContactName"] ?? "OneAuthCenter",
                Email = swaggerSettings["ContactEmail"] ?? "yanguobiao@OneAuthCenter.com"
            }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT 授权头使用 Bearer 方案。示例: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
    });
}
builder.Services.AddConfiguredDatabase(builder.Configuration);
//多数据库
builder.Services.AddDbContext<SerilogDemoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetSection("DatabaseSettings").GetValue<string>("ConnectionStrings:SerilogDemo"))
    );

// 配置 Serilog
builder.Services.AddSerilogConfigure(builder.Configuration);
//配置RabbitMQ
builder.Services.AddRabbitMqConfigure(builder.Configuration);
//配置AI服务
builder.Services.AddAIServiceColletction(builder.Configuration);
// 注册签名凭证服务（类似 IdentityServer 的 AddDeveloperSigningCredential）
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var useTestCert = jwtSettings.GetValue<bool>("SigningCertificate:UseTestCertificate");
var certPath = jwtSettings["SigningCertificate:CertificatePath"];
var certPassword = jwtSettings["SigningCertificate:CertificatePassword"];
var signingCredentialsService = new SigningCredentialsService(useTestCert, certPath, certPassword);
builder.Services.AddSingleton(signingCredentialsService);
// 配置 JWT 认证（使用 RSA 公钥验证）
// 注意：Issuer 和 Audience 在运行时动态获取（类似 IdentityServer4）
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // 开发环境可以设为 false
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingCredentialsService.GetValidationKey(), // 使用 RSA 公钥
        ValidateIssuer = false, // 动态 Issuer，在 Token 生成时设置
        ValidateAudience = false, // 动态 Audience，在 Token 生成时设置
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = JwtBearerEventsHelper.AuthenticationFailedHandler,
        OnTokenValidated = JwtBearerEventsHelper.TokenValidatedHandler,
        OnMessageReceived = JwtBearerEventsHelper.MessageReceivedHandler,
        OnChallenge = JwtBearerEventsHelper.ChallengeHandler,
        OnForbidden = JwtBearerEventsHelper.ForbiddenHandler
    };
});
builder.Services.AddAuthorization();
//redis
builder.Services.AddTransient<RedisStringService>();
builder.Services.AddTransient<RedisListService>();
builder.Services.AddTransient<RedisHashService>();
builder.Services.AddTransient<RedisSetService>();
builder.Services.AddTransient<RedisZSetService>();
// 注册仓储
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuthorizationCodeRepository, AuthorizationCodeRepository>();
builder.Services.AddScoped<IScopeRepository, ScopeRepository>();
builder.Services.AddScoped<IUserConsentRepository, UserConsentRepository>();
builder.Services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();

// 注册服务
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IUserService, UserService>();
//注册RabbbitMQ服务
builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();
//
builder.Services.AddScoped<ChatService>();
// 添加 HttpContextAccessor（用于在服务中访问 HTTP 上下文）
builder.Services.AddHttpContextAccessor();
var app = builder.Build();
// 配置 HTTP 请求管道
// Swagger 根据配置文件启用/禁用（不依赖环境变量）
if (swaggerSettings.GetValue<bool>("Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        var title = swaggerSettings["Title"] ?? "OneAuthCenter API111";
        var version = swaggerSettings["Version"] ?? "V1";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{title} {version}");//c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{title} {version}")
        c.RoutePrefix = string.Empty;//string.Empty; // 将 Swagger UI 设置为根路径
    });

    Console.WriteLine($"✓ Swagger UI: 已启用 ({app.Configuration["SwaggerSettings:Title"]})");
}
else
{
    Console.WriteLine("✗ Swagger UI: 已禁用（生产环境）");
}
app.UseExceptionMiddleware();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("Fixed");

app.Run();
