using LYBT.Infrastructure.Configuration.Extensions;
using LYBT.Infrastructure.Configuration.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 数据库与基础设施服务注册扩展
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分
/// 职责：数据库配置、缓存配置、健康检查
/// </summary>
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// 注册基础设施服务
    /// </summary>
    public static IServiceCollection RegisterInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // =========== UltraThink Phase 2：统一配置管理 ===========
        // 注册新的统一配置系统，同时保持向后兼容
        services.AddLybtConfiguration(configuration);

        // 配置验证由IValidateOptions<LybtOptions>自动处理

        // 数据库配置 - 从统一配置读取
        var lybtOptions = configuration.GetLybtOptions();
        var connectionString = lybtOptions.Database.ConnectionString ??
                              configuration.GetConnectionString("DefaultConnection") ??
                              Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                              string.Empty;

        // 缓存配置 - Issue #1754: 直接使用IMemoryCache，移除ICacheService抽象层
        services.Configure<MemoryCacheOptions>(options =>
        {
            var sizeLimit = lybtOptions.MemoryCache.SizeLimit;
            if (sizeLimit > 0)
            {
                options.SizeLimit = sizeLimit;
                options.CompactionPercentage = lybtOptions.MemoryCache.CompactionPercentage;
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(lybtOptions.MemoryCache.ExpirationScanFrequencySeconds);
            }
        });
        services.AddMemoryCache(); // 添加IMemoryCache服务

        // 响应缓存配置
        services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 100_000_000;  // 100MB
            options.UseCaseSensitivePaths = false;
        });

        // 输出缓存配置（.NET 7+）
        services.AddOutputCache(options =>
        {
            // 默认策略
            options.AddBasePolicy(builder =>
                builder.Expire(TimeSpan.FromMinutes(5)));

            // 草药数据缓存1小时
            options.AddPolicy("HerbsCache", builder =>
                builder.Expire(TimeSpan.FromHours(1))
                       .Tag("herbs"));

            // 配方模板缓存2小时
            options.AddPolicy("FormulasCache", builder =>
                builder.Expire(TimeSpan.FromHours(2))
                       .Tag("formulas"));

            // 患者数据缓存策略（30分钟）
            options.AddPolicy("PatientsCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(30))
                       .Tag("patients"));

            // 处方缓存策略（10分钟，更新频繁）
            options.AddPolicy("PrescriptionsCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(10))
                       .Tag("prescriptions"));

            // 病例缓存策略（20分钟）
            options.AddPolicy("MedicalCaseCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(20))
                       .Tag("medicalcases"));

            // 用户权限缓存10分钟
            options.AddPolicy("UserPermissionsCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(10))
                       .Tag("permissions"));
        });

        // =========== 保持向后兼容：注册传统配置选项 ===========
        // 注意：这些配置选项已通过 AddLybtConfiguration 自动映射和注册
        // 这里仅显式验证关键配置选项以确保启动时验证

        // 验证 JWT 配置
        // Issue #1761 Phase 3.1: Authentication.Jwt → Jwt（完全扁平化）
        if (string.IsNullOrEmpty(lybtOptions.Jwt.SecretKey))
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("生产环境必须配置 JWT 密钥。");
            }
        }

        // 验证数据库连接 - 仅记录警告，不阻塞启动
        if (string.IsNullOrEmpty(connectionString))
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            Console.WriteLine($"[WARNING] 数据库连接字符串未配置 (Environment: {environment})");

            // 开发环境使用默认连接字符串
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=20;Min Pool Size=2;Pooling=true";
                Console.WriteLine("[INFO] 开发环境使用默认数据库连接字符串");
            }
        }

        // 注册 AppDbContext - 无论连接字符串是否存在都需要注册
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<LYBT.Infrastructure.Data.AppDbContext>((serviceProvider, options) =>
            {
                var sqlOptions = options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(
                        lybtOptions.Database.RetryPolicy.MaxRetryCount,
                        TimeSpan.FromMilliseconds(lybtOptions.Database.RetryPolicy.MaxDelayMs),
                        null);
                });

                // Issue #1761 Phase 2.1: 使用硬编码默认值，移除Monitoring和ConnectionPool配置依赖
                // MVP阶段：开发环境默认启用详细日志，生产环境关闭敏感数据
                options.EnableSensitiveDataLogging(false); // 生产环境默认关闭
                options.EnableDetailedErrors(true); // 开发环境启用详细错误
                options.EnableServiceProviderCaching();

                // 设置命令超时（默认30秒）
                options.UseSqlServer(opt => opt.CommandTimeout(30));
            });
        }
        else
        {
            // 即使没有连接字符串也注册 AppDbContext，以避免 DI 错误
            services.AddDbContext<LYBT.Infrastructure.Data.AppDbContext>(options =>
            {
                Console.WriteLine("[WARNING] AppDbContext 注册时没有可用的数据库连接字符串");
            });
        }

        // 常用服务
        services.AddHttpContextAccessor();
        services.AddScoped<LYBT.Infrastructure.Configuration.Services.DefaultPasswordService>();
        services.AddScoped<LYBT.Infrastructure.Data.DatabaseInitializationService>();

        // Issue #1726 Phase 3: 数据库健康检查与启动诊断
        services.AddHealthChecks()
            .AddCheck<LYBT.WebAPI.HealthCheck.SqlServerHealthCheck>("database");
        services.AddHostedService<LYBT.WebAPI.HealthCheck.DatabaseStartupDiagnostics>();

        return services;
    }
}
