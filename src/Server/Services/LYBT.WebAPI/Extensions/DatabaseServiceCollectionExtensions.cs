using LYBT.Infrastructure.DependencyInjection;
using LYBT.Infrastructure.Logging;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LybtMemoryCacheOptions = LYBT.Shared.Configuration.Options.Server.MemoryCacheOptions;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 数据库与基础设施服务注册扩展
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分
/// 职责：数据库配置、缓存配置、健康检查
/// unify-configuration-system: 迁移到 LYBT.Shared.Configuration
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
        // unify-configuration-system: 使用强类型配置
        var databaseOptions = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(databaseOptions);

        var memoryCacheOptions = new LybtMemoryCacheOptions();
        configuration.GetSection(LybtMemoryCacheOptions.SectionName).Bind(memoryCacheOptions);

        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);

        // 数据库配置 - 从统一配置读取
        var connectionString = databaseOptions.ConnectionString ??
                              configuration.GetConnectionString("DefaultConnection") ??
                              Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                              string.Empty;

        // 缓存配置 - 配置Microsoft内置MemoryCacheOptions
        services.Configure<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>(options =>
        {
            var sizeLimit = memoryCacheOptions.SizeLimit;
            if (sizeLimit > 0)
            {
                options.SizeLimit = sizeLimit;
                options.CompactionPercentage = memoryCacheOptions.CompactionPercentage;
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(memoryCacheOptions.ExpirationScanFrequencySeconds);
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

            // 草材数据智能缓存：支持搜索参数区分，缓存30分钟
            // 不同搜索条件(page, pageSize, keyword, category)会有独立缓存
            options.AddPolicy("HerbsCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(30))
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

        // unify-configuration-system: 验证关键配置
        // 验证 JWT 配置
        if (string.IsNullOrEmpty(jwtOptions.SecretKey))
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
                    // unify-configuration-system: 使用强类型配置
                    sqlOptions.EnableRetryOnFailure(
                        databaseOptions.RetryPolicy.MaxRetryCount,
                        TimeSpan.FromMilliseconds(databaseOptions.RetryPolicy.MaxDelayMs),
                        null);
                });

                // unify-configuration-system: 使用硬编码默认值，移除Monitoring和ConnectionPool配置依赖
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

        // Phase 1: 注册泛型Repository基础设施
        services.AddServerRepositories();

        // 常用服务
        services.AddHttpContextAccessor();

        // unify-configuration-system: 注册 Options 供 DefaultPasswordService 和 DatabaseInitializationService 使用
        services.Configure<DefaultPasswordOptions>(
            configuration.GetSection(DefaultPasswordOptions.SectionName));
        services.Configure<SystemAdminOptions>(
            configuration.GetSection(SystemAdminOptions.SectionName));

        services.AddScoped<LYBT.Infrastructure.Configuration.Services.DefaultPasswordService>();
        services.AddScoped<LYBT.Infrastructure.Data.DatabaseInitializationService>();

        // OpenSpec: decouple-server-modules - 跨模块服务 ISP 拆分 (D5-1)
        services.AddScoped<LYBT.Infrastructure.Services.CrossModuleService>();
        services.AddScoped<LYBT.Infrastructure.Services.CrossModule.IPatientCrossModuleService>(sp => sp.GetRequiredService<LYBT.Infrastructure.Services.CrossModuleService>());
        services.AddScoped<LYBT.Infrastructure.Services.CrossModule.IHerbCrossModuleService>(sp => sp.GetRequiredService<LYBT.Infrastructure.Services.CrossModuleService>());
        services.AddScoped<LYBT.Infrastructure.Services.CrossModule.IUserCrossModuleService>(sp => sp.GetRequiredService<LYBT.Infrastructure.Services.CrossModuleService>());
        services.AddScoped<LYBT.Infrastructure.Services.CrossModule.ICrossModuleAuthService>(sp => sp.GetRequiredService<LYBT.Infrastructure.Services.CrossModuleService>());
        // 旧接口保留兼容 (标记 [Obsolete])
        services.AddScoped<LYBT.Infrastructure.Services.ICrossModuleService>(sp => sp.GetRequiredService<LYBT.Infrastructure.Services.CrossModuleService>());

        // Issue #1726 Phase 3: 数据库健康检查与启动诊断
        services.AddHealthChecks()
            .AddCheck<LYBT.WebAPI.HealthCheck.SqlServerHealthCheck>("database");
        services.AddHostedService<LYBT.WebAPI.HealthCheck.DatabaseStartupDiagnostics>();

        // Issue #1873: 安全审计日志清理后台服务
        services.AddHostedService<LYBT.WebAPI.BackgroundServices.SecurityAuditCleanupService>();

        // refactor-logging-system: 日志清理后台服务
        services.Configure<LYBT.Infrastructure.Logging.LogCleanupOptions>(
            configuration.GetSection(LYBT.Infrastructure.Logging.LogCleanupOptions.SectionName));
        services.AddHostedService<LogCleanupService>();

        return services;
    }
}
