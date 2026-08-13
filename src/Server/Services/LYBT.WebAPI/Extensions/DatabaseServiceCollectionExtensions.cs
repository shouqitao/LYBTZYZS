using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.WebAPI.HealthCheck;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.EntityFrameworkCore;
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

        // 数据库配置 - 从统一配置读取（HEALTHCHECK-FALLBACK-FIX: 共享 resolver——与 SqlServerHealthCheck 同源）
        var connectionString = DatabaseConnectionResolver.Resolve(configuration, databaseOptions);

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

        // 输出缓存注册（P-01）：ASP.NET Core 8 默认不缓存带 [Authorize] 的响应，
        // 原 5 个策略（HerbsCache/FormulasCache/PatientsCache/PrescriptionsCache/MedicalCaseCache/UserPermissionsCache）
        // 标注的端点全部带 [Authorize]，缓存从未命中，策略定义已全部删除。
        // 保留裸 AddOutputCache() 仅使 IOutputCacheStore 可解析 —— CacheInvalidationService 依赖它按 tag 驱逐
        // （实际生效的失效走 IMemoryCache.RemoveByPrefix，见 CacheInvalidationService）。
        services.AddOutputCache();

        // 缓存失效服务
        services.AddSingleton<LYBT.Infrastructure.Caching.ICacheInvalidationService, LYBT.Infrastructure.Caching.CacheInvalidationService>();

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

        // A1-03: 连接字符串缺失时直接抛出异常，禁止 fallback 硬编码
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "数据库连接字符串未配置。请在 appsettings.json 的 Database:ConnectionString " +
                "或 ConnectionStrings:DefaultConnection 中配置，或设置 CONNECTION_STRING 环境变量。");
        }

        // 注册 AppDbContext（connectionString 已通过上方检查，必定非空）
        services.AddDbContext<LYBT.Infrastructure.Data.AppDbContext>((serviceProvider, options) =>
        {
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
                // unify-configuration-system: 使用强类型配置
                sqlOptions.EnableRetryOnFailure(
                    databaseOptions.RetryPolicy.MaxRetryCount,
                    TimeSpan.FromMilliseconds(databaseOptions.RetryPolicy.MaxDelayMs),
                    null);
                sqlOptions.CommandTimeout(databaseOptions.ConnectionPool.CommandTimeoutSeconds);
                // 2026-08-13 真机失败真正根因（splitquery-fix）: 全局 SplitQuery → 所有含 Include 的查询
                // 拆多条 SQL 独立连接——远程 SQL（192.168.190.243，延迟 ~0.42s）多连接 → 连接失败/超时 →
                // 种子失败/保存异常（本地 SQLite/低延迟测不出）。改回 EF 默认 SingleQuery（JOIN 一条 SQL）——
                // 确有需要拆分的查询（大 Include 集合笛卡尔爆炸）在查询级用 AsSplitQuery()（按需，不全局）。
            });

            options.EnableSensitiveDataLogging(false);
            // 生产环境禁用详细错误，防止泄露数据库架构
            options.EnableDetailedErrors(environment.IsDevelopment());
            options.EnableServiceProviderCaching();
        });

        // P-10: 注册 IDbContextAccessor 避免直接注入 AppDbContext
        services.AddScoped<IDbContextAccessor, DbContextAccessor>();

        // 常用服务
        services.AddHttpContextAccessor();

        services.AddScoped<LYBT.Infrastructure.Data.DatabaseInitializationService>();

        // Architecture Fix: 注册健康检查服务 (Task 1.1)
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        // Issue #1726 Phase 3: 数据库健康检查与启动诊断
        services.AddHealthChecks()
            .AddCheck<LYBT.WebAPI.HealthCheck.SqlServerHealthCheck>("database");
        services.AddHostedService<LYBT.WebAPI.HealthCheck.DatabaseStartupDiagnostics>();

        // refactor-logging-system: 日志清理后台服务
        services.AddHostedService<LogCleanupService>();

        return services;
    }
}


