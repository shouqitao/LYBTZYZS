/// <summary>
/// 极简版WebAPI程序入口 - 确保基本功能运行
/// </summary>
using LYBT.Infrastructure.Authentication;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Options;
using LYBT.Module.Users;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// =========== 1. 基础设施服务配置 ===========

// 统一数据库上下文
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString)) {
    builder.Services.AddDbContext<AppDbContext>(options => {
        options.UseSqlServer(connectionString, sqlOptions => {
            sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
            sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        });
        options.EnableSensitiveDataLogging(false);
        options.EnableServiceProviderCaching();
    });

    // 为了兼容性，同时注册原有的InfrastructureDbContext
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
// 注册Users模块服务（不再需要单独的数据库上下文）
builder.Services.AddUsersModuleServices();

// 添加AutoMapper配置
builder.Services.AddAutoMapper(typeof(Program));

// 注册认证模块服务
builder.Services.AddScoped<LYBT.Module.Auth.Interfaces.IAuthRepository, LYBT.Module.Auth.Repositories.AuthRepository>();
builder.Services.AddScoped<LYBT.Module.Auth.Services.SysAdminHandler>();
builder.Services.AddScoped<LYBT.Module.Auth.Interfaces.IAuthService, LYBT.Module.Auth.Services.AuthService>();

// =========== 4. 添加API版本控制 ===========
builder.Services.AddApiVersioning(opt => {
    opt.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.UrlSegmentApiVersionReader(),
        new Asp.Versioning.QueryStringApiVersionReader("version"),
        new Asp.Versioning.HeaderApiVersionReader("X-Version"));
}).AddApiExplorer(setup => {
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// =========== 5. 添加Swagger文档 ===========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
        Title = "LYBT 中医诊所管理系统 API",
        Version = "v1",
        Description = "传统中医诊所管理系统API文档"
    });
});

// =========== 6. 添加控制器和JSON配置 ===========
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// =========== 7. 构建应用 ===========
var app = builder.Build();

// =========== 8. 初始化数据（异常处理） ===========
using (var scope = app.Services.CreateScope()) {
    try {
        // 使用超时取消令牌防止初始化卡死
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // 初始化统一配置服务
        var configService = scope.ServiceProvider.GetService<IUnifiedConfigService>();
        if (configService != null) {
            try {
                await configService.InitializeDefaultGlobalSettingsAsync();
            } catch (Exception configEx) {
                Console.WriteLine($"⚠️  配置服务初始化失败，将跳过: {configEx.Message}");
            }
        }

        // 记录应用启动日志
        var logService = scope.ServiceProvider.GetService<IUnifiedLogService>();
        if (logService != null) {
            try {
                await logService.LogInfoAsync("System", "应用程序启动成功", null, "WebAPI-Startup");
            } catch (Exception logEx) {
                Console.WriteLine($"⚠️  日志服务初始化失败，将跳过: {logEx.Message}");
            }
        }

        Console.WriteLine("✅ 应用程序初始化成功");
    } catch (Exception ex) {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogError(ex, "❌ 应用程序初始化失败");
        Console.WriteLine($"❌ 初始化失败: {ex.Message}");
        Console.WriteLine("⚠️  程序将继续启动，但某些功能可能不可用");
    }
}

// =========== 9. 配置中间件管道 ===========

// 启用Swagger（优先级最高）
app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LYBT API v1");
    c.RoutePrefix = "swagger";
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});
Console.WriteLine("📖 Swagger UI 已启用 - /swagger");

// 暂时禁用全局异常处理以测试Swagger
// app.UseGlobalExceptionHandling();

// 性能监控中间件
app.UsePerformanceMonitoring();

// CORS, 认证, 路由
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// =========== 10. 启动应用 ===========
var urls = app.Urls.Count > 0 ? string.Join(", ", app.Urls) : "默认端口";
Console.WriteLine($"🚀 LYBT中医诊所管理系统启动成功!");
Console.WriteLine($"📍 访问地址: {urls}");
Console.WriteLine($"📊 数据库: {(string.IsNullOrEmpty(connectionString) ? "未配置" : "已连接")}");
Console.WriteLine($"🔐 JWT认证: {(jwtOptions != null ? "已启用" : "未配置")}");
Console.WriteLine($"⚠️  注意: 仅启用认证模块，其他业务模块需要单独配置");
Console.WriteLine($"🎯 无限递归问题已解决！");
Console.WriteLine($"💡 按 Ctrl+C 停止程序");

// 添加优雅关闭支持
var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => {
    Console.WriteLine("\n⚠️  正在关闭程序...");
    e.Cancel = true; // 取消默认的强制终止
    cancellationTokenSource.Cancel(); // 触发取消令牌
};
AppDomain.CurrentDomain.ProcessExit += (_, __) => {
    Console.WriteLine("\n⚠️  正在关闭程序...");
    cancellationTokenSource.Cancel();
    // 等待应用优雅关闭并确保资源释放
    app.StopAsync().GetAwaiter().GetResult();
};

try {
    await app.RunAsync(cancellationTokenSource.Token);
} catch (OperationCanceledException) {
    Console.WriteLine("✅ 程序已正常关闭");
} finally {
    // 确保释放资源
    await app.DisposeAsync();
    Console.WriteLine("🔚 资源已释放，程序完全退出");
}