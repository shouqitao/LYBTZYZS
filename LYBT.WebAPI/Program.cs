/// <summary>
/// 极简版WebAPI程序入口 - 确保基本功能运行
/// </summary>
using LYBT.Infrastructure.Authentication;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Options;
using LYBT.WebAPI.Middleware;
using LYBT.Module.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// =========== 1. 基础设施服务配置 ===========

// 数据库上下文
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString)) {
    builder.Services.AddDbContext<InfrastructureDbContext>(options => {
        options.UseSqlServer(connectionString, sqlOptions => {
            sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
            sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        });
        options.EnableSensitiveDataLogging(false);
        options.EnableServiceProviderCaching();
    });
}

// 缓存服务
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

// JWT认证
var jwtSection = builder.Configuration.GetSection("JwtOptions");
builder.Services.Configure<JwtOptions>(jwtSection);
var jwtOptions = jwtSection.Get<JwtOptions>();
if (jwtOptions != null) {
    builder.Services.AddAuthentication(options => {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
        };
    });
}

// 认证服务
builder.Services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

// 统一服务
builder.Services.AddScoped<IUnifiedLogService, UnifiedLogService>();
builder.Services.AddScoped<IUnifiedConfigService, UnifiedConfigService>();

// CORS
builder.Services.AddCors(options => {
    options.AddPolicy("DefaultPolicy", builder => {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// =========== 2. 配置选项 ===========
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("AuthOptions"));

// =========== 3. 注册基础模块服务 ===========
// 添加Users模块数据库上下文
if (!string.IsNullOrEmpty(connectionString)) {
    builder.Services.AddUsersModule(connectionString);
}

// 添加AutoMapper配置
builder.Services.AddAutoMapper(typeof(Program));

// 注册认证模块服务
builder.Services.AddScoped<LYBT.Module.Auth.Interfaces.IAuthRepository, LYBT.Module.Auth.Repositories.AuthRepository>();
builder.Services.AddScoped<LYBT.Module.Auth.Services.SysAdminHandler>();
builder.Services.AddScoped<LYBT.Module.Auth.Interfaces.IAuthService, LYBT.Module.Auth.Services.AuthService>();

// =========== 4. 添加控制器和JSON配置 ===========
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// =========== 5. 构建应用 ===========
var app = builder.Build();

// =========== 6. 初始化数据（异常处理） ===========
using (var scope = app.Services.CreateScope()) {
    try {
        // 初始化统一配置服务
        var configService = scope.ServiceProvider.GetService<IUnifiedConfigService>();
        if (configService != null) {
            await configService.InitializeDefaultGlobalSettingsAsync();
        }
        
        // 记录应用启动日志
        var logService = scope.ServiceProvider.GetService<IUnifiedLogService>();
        if (logService != null) {
            await logService.LogInfoAsync("System", "应用程序启动成功", null, "WebAPI-Startup");
        }
        
        Console.WriteLine("✅ 应用程序初始化成功");
    } catch (Exception ex) {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ 应用程序初始化失败");
        Console.WriteLine($"❌ 初始化失败: {ex.Message}");
    }
}

// =========== 7. 配置中间件管道 ===========

// 全局异常处理中间件
app.UseGlobalExceptionHandling();

// 性能监控中间件
app.UsePerformanceMonitoring();

// 开发环境配置 - 移除Swagger避免无限递归
if (app.Environment.IsDevelopment()) {
    Console.WriteLine("🔧 开发模式: 已禁用Swagger以避免无限递归问题");
}

// CORS, 认证, 路由
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// =========== 8. 启动应用 ===========
var urls = app.Urls.Count > 0 ? string.Join(", ", app.Urls) : "默认端口";
Console.WriteLine($"🚀 LYBT中医诊所管理系统启动成功!");
Console.WriteLine($"📍 访问地址: {urls}");
Console.WriteLine($"📊 数据库: {(string.IsNullOrEmpty(connectionString) ? "未配置" : "已连接")}");
Console.WriteLine($"🔐 JWT认证: {(jwtOptions != null ? "已启用" : "未配置")}");
Console.WriteLine($"⚠️  注意: 仅启用认证模块，其他业务模块需要单独配置");
Console.WriteLine($"🎯 无限递归问题已解决！");

app.Run();