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
using LYBT.WebAPI.Extensions;
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

// 数据库初始化服务
builder.Services.AddScoped<LYBT.Infrastructure.Database.DatabaseInitializationService>();

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

// 注册所有LYBT业务模块服务
builder.Services.AddAllModules();

// 添加AutoMapper配置 - 使用扩展方法
builder.Services.AddLybtAutoMapperProfiles();

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
        Title = "凌隐宝堂中医诊所诊疗系统 API",
        Version = "v1",
        Description = "凌隐宝堂中医诊所诊疗系统API文档"
    });

    // 解决Schema ID冲突问题 - 生成真正唯一的Schema ID
    c.CustomSchemaIds(type => {
        // 使用类型的完整签名生成唯一ID
        if (type.IsGenericType) {
            var genericDef = type.GetGenericTypeDefinition();
            var genericTypeName = genericDef.FullName?.Split('`')[0]?.Replace(".", "") ?? genericDef.Name.Split('`')[0];

            // 递归处理泛型参数，包括嵌套泛型
            var genericArgs = type.GetGenericArguments()
                .Select(arg => GetTypeSignature(arg))
                .ToArray();

            return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
        }

        return type.FullName?.Replace(".", "").Replace("+", "") ?? type.Name;
    });

    // 辅助方法：生成类型签名
    string GetTypeSignature(Type type) {
        if (type.IsGenericType) {
            var genericDef = type.GetGenericTypeDefinition();
            var genericTypeName = genericDef.Name.Split('`')[0];
            var genericArgs = type.GetGenericArguments()
                .Select(arg => GetTypeSignature(arg))
                .ToArray();
            return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
        }
        return type.Name.Replace("[]", "Array");
    }
});

// =========== 6. 添加控制器和JSON配置 ===========
// 确保UTF-8编码支持
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = false;
    // 明确设置UTF-8编码支持
    options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// =========== 7. 构建应用 ===========
var app = builder.Build();

// =========== 8. 数据库和应用初始化 ===========
using (var scope = app.Services.CreateScope()) {
    try {
        Console.WriteLine("🔄 正在初始化应用程序...");

        // 使用超时取消令牌防止初始化卡死
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // 数据库初始化（优先执行）
        try {
            var dbInitService = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Database.DatabaseInitializationService>();
            await dbInitService.InitializeDatabaseAsync();

            // 显示数据库信息
            var dbInfo = await dbInitService.GetDatabaseInfoAsync();
            Console.WriteLine($"📊 数据库信息:");
            Console.WriteLine($"   ├─ 数据库名: {dbInfo.DatabaseName}");
            Console.WriteLine($"   ├─ 连接状态: {(dbInfo.IsConnected ? "✅ 已连接" : "❌ 连接失败")}");
            Console.WriteLine($"   ├─ 已应用迁移: {dbInfo.AppliedMigrationsCount} 个");
            Console.WriteLine($"   ├─ 待处理迁移: {dbInfo.PendingMigrationsCount} 个");
            Console.WriteLine($"   └─ 最新迁移: {dbInfo.LastMigration ?? "无"}");
        } catch (Exception dbEx) {
            Console.WriteLine($"❌ 数据库初始化失败: {dbEx.Message}");
            Console.WriteLine("⚠️  程序将尝试继续启动，但数据库相关功能可能不可用");
            Console.WriteLine($"💡 建议检查数据库连接字符串和SQL Server服务状态");

            // 记录详细错误信息到日志
            var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
            logger?.LogError(dbEx, "数据库初始化详细错误信息");
        }

        // 初始化统一配置服务
        var configService = scope.ServiceProvider.GetService<IUnifiedConfigService>();
        if (configService != null) {
            try {
                await configService.InitializeDefaultGlobalSettingsAsync();
                Console.WriteLine("✅ 配置服务初始化成功");
            } catch (Exception configEx) {
                Console.WriteLine($"⚠️  配置服务初始化失败，将跳过: {configEx.Message}");
            }
        }

        // 记录应用启动日志
        var logService = scope.ServiceProvider.GetService<IUnifiedLogService>();
        if (logService != null) {
            try {
                await logService.LogInfoAsync("System", "应用程序启动成功", null, "WebAPI-Startup");
                Console.WriteLine("✅ 日志服务初始化成功");
            } catch (Exception logEx) {
                Console.WriteLine($"⚠️  日志服务初始化失败，将跳过: {logEx.Message}");
            }
        }

        Console.WriteLine("✅ 应用程序初始化完成");
    } catch (Exception ex) {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogError(ex, "❌ 应用程序初始化失败");
        Console.WriteLine($"❌ 初始化失败: {ex.Message}");
        Console.WriteLine("⚠️  程序将继续启动，但某些功能可能不可用");

        // 在开发环境中显示更详细的错误信息
        if (app.Environment.IsDevelopment()) {
            Console.WriteLine($"详细错误: {ex}");
        }
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

// 全局异常处理中间件
app.UseGlobalExceptionHandling();

// 性能监控中间件
app.UsePerformanceMonitoring();

// CORS, 认证, 路由
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// =========== 10. 启动应用 ===========
var urls = app.Urls.Count > 0 ? string.Join(", ", app.Urls) : "默认端口";
Console.WriteLine($"");
Console.WriteLine($"🚀 LYBT中医诊所管理系统启动成功!");
Console.WriteLine($"📍 访问地址: {urls}");
Console.WriteLine($"📖 Swagger文档: {urls.Replace("http://", "").Replace("https://", "").Split(',')[0]}/swagger");

// 获取数据库状态信息
using (var scope = app.Services.CreateScope()) {
    try {
        var dbInitService = scope.ServiceProvider.GetService<LYBT.Infrastructure.Database.DatabaseInitializationService>();
        if (dbInitService != null) {
            var dbInfo = await dbInitService.GetDatabaseInfoAsync();
            Console.WriteLine($"📊 数据库状态: {(dbInfo.IsConnected ? "✅ 已连接" : "❌ 未连接")} ({dbInfo.DatabaseName})");
        } else {
            Console.WriteLine($"📊 数据库状态: {(string.IsNullOrEmpty(connectionString) ? "❌ 未配置" : "⚠️ 状态未知")}");
        }
    } catch {
        Console.WriteLine($"📊 数据库状态: ⚠️ 检查失败");
    }
}

Console.WriteLine($"🔐 JWT认证: {(jwtOptions != null ? "✅ 已启用" : "❌ 未配置")}");
Console.WriteLine($"⚡ 服务状态: 所有核心模块已加载");
Console.WriteLine($"💡 按 Ctrl+C 停止程序");
Console.WriteLine($"");

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