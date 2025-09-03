using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure;
using LYBT.Module.Users;
using LYBT.Module.Auth;
using LYBT.WebAPI.Services;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 统一服务注册管理 - UltraThink统一注入管理系统
/// 将所有服务注册逻辑统一到一个地方，提高可维护性和可读性
/// </summary>
public static class UnifiedServiceRegistration {
    /// <summary>
    /// 注册所有应用服务（统一入口）
    /// </summary>
    public static IServiceCollection RegisterAllApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment) {
        // 1. 基础设施服务
        services.RegisterInfrastructureServices(configuration);

        // 2. 认证和安全服务
        services.RegisterAuthenticationServices(configuration);

        // 3. 业务模块服务
        services.RegisterBusinessModules();

        // 4. API和文档服务
        services.RegisterApiServices();

        // 5. 控制器和JSON配置
        services.RegisterControllerServices();

        // 6. 跨域策略
        services.AddSecureCorsPolicy(configuration, environment);

        return services;
    }

    /// <summary>
    /// 注册基础设施服务
    /// </summary>
    private static IServiceCollection RegisterInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration) {
        // =========== 简化配置管理系统 - UltraThink重构 ===========
        // 使用简化配置服务，替代复杂的ConfigurationManager/EnvironmentManager/SecretManager
        services.AddScoped<ISimplifiedConfigurationService, SimplifiedConfigurationService>();

        // =========== 统一数据库上下文 ===========
        var configService = services.BuildServiceProvider().GetRequiredService<ISimplifiedConfigurationService>();
        var connectionString = configService.GetConnectionString();

        if (!string.IsNullOrEmpty(connectionString)) {
            services.AddDbContext<AppDbContext>(options => {
                options.UseSqlServer(connectionString, sqlOptions => {
                    sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                });

                var dbOptions = configService.GetSection<DatabaseOptions>("DatabaseOptions");
                options.EnableSensitiveDataLogging(dbOptions?.EnableSensitiveDataLogging ?? false);
                options.EnableDetailedErrors(dbOptions?.EnableDetailedErrors ?? false);
                options.EnableServiceProviderCaching();

                if (dbOptions?.CommandTimeout > 0) {
                    options.UseSqlServer(opt => opt.CommandTimeout(dbOptions.CommandTimeout));
                }
            });
        }

        // =========== 缓存服务 - UltraThink简化版 ===========
        services.AddMemoryCache(options => {
            var cacheOptions = configService.GetSection<CacheOptions>("CacheOptions");
            if (cacheOptions?.MemoryCache != null) {
                options.SizeLimit = cacheOptions.MemoryCache.SizeLimit;
                options.CompactionPercentage = cacheOptions.MemoryCache.CompactionPercentage;
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(cacheOptions.MemoryCache.ExpirationScanFrequency);
            }
        });

        // =========== 配置选项绑定 - UltraThink简化版 ===========
        // 使用简化配置服务，自动处理环境变量覆盖
        services.Configure<SysAdminOptions>(options => {
            var adminPassword = configService.GetAdminPassword();
            options.DefaultPassword = adminPassword;
            configuration.GetSection("SysAdminOptions").Bind(options);
        });

        services.Configure<LYBT.Module.Users.UserOptions>(options => {
            var userPassword = configService.GetUserDefaultPassword();
            options.DefaultUserPassword = userPassword;
            configuration.GetSection("UserOptions").Bind(options);
        });

        // =========== 安全配置服务 ===========
        services.AddScoped<IPasswordValidationService, PasswordValidationService>();
        services.AddScoped<ISecurityConfigurationValidator, SecurityConfigurationValidator>();

        // =========== 监控和健康检查服务 ===========
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        services.AddSingleton<ISystemMetricsCollector, SystemMetricsCollector>();

        // =========== 统一服务 ===========
        // 注意：日志系统已简化为标准ILogger，无需单独注册

        // =========== 性能优化服务 - UltraThink简化版 ===========
        services.RegisterPerformanceServices(configService);

        // =========== 日志和监控服务 - UltraThink简化版 ===========
        services.RegisterLoggingAndMonitoringServices(configService);

        // =========== 数据库初始化服务 ===========
        services.AddScoped<LYBT.Infrastructure.Data.DatabaseInitializationService>();

        return services;
    }

    /// <summary>
    /// 注册性能优化服务 - UltraThink简化版本
    /// 移除过度设计的性能组件，使用.NET内置服务
    /// </summary>
    private static IServiceCollection RegisterPerformanceServices(
        this IServiceCollection services,
        ISimplifiedConfigurationService configService) {
        // =========== 简化缓存管理 ===========
        // UltraThink简化：使用内置IMemoryCache替代复杂的UnifiedCacheManager
        services.AddMemoryCache(options => {
            // 使用默认配置，避免复杂的配置选项
            options.SizeLimit = 1000; // 简化：使用固定的缓存大小限制
        });

        // =========== 数据库性能优化 ===========
        // UltraThink v2.0: 禁用复杂数据库性能优化 - 20人以下小诊所不需要复杂的数据库性能监控和优化
        // services.AddScoped<LYBT.Infrastructure.Performance.Database.IUnifiedDatabaseOptimizer, LYBT.Infrastructure.Performance.Database.UnifiedDatabaseOptimizer>();

        // =========== 异步处理管理 ===========
        // UltraThink简化：移除复杂的异步处理器，使用简单的后台服务即可
        // services.AddSingleton<LYBT.Infrastructure.Performance.Async.IUnifiedAsyncProcessor, LYBT.Infrastructure.Performance.Async.UnifiedAsyncProcessor>();
        // services.AddHostedService(provider => (LYBT.Infrastructure.Performance.Async.UnifiedAsyncProcessor)provider.GetRequiredService<LYBT.Infrastructure.Performance.Async.IUnifiedAsyncProcessor>());

        return services;
    }

    /// <summary>
    /// 注册认证和安全服务
    /// </summary>
    private static IServiceCollection RegisterAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration) {
        // =========== JWT认证配置 - UltraThink简化版 ===========
        var serviceProvider = services.BuildServiceProvider();
        var configService = serviceProvider.GetRequiredService<ISimplifiedConfigurationService>();

        try {
            // 获取JWT配置
            var jwtSection = configuration.GetSection("JwtOptions");
            var jwtOptions = jwtSection.Get<JwtOptions>()
                ?? new LYBT.Infrastructure.Configuration.Options.JwtOptions();

            // 使用简化配置服务获取JWT密钥
            jwtOptions.Secret = configService.GetJwtSecret();

            // 注册处理过的JwtOptions到DI容器
            services.AddSingleton<Microsoft.Extensions.Options.IOptions<JwtOptions>>(
                new Microsoft.Extensions.Options.OptionsWrapper<JwtOptions>(jwtOptions));

            if (!string.IsNullOrEmpty(jwtOptions.Secret)) {
                services.AddAuthentication(options => {
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
            } else {
                throw new InvalidOperationException("JWT密钥为空，无法配置认证");
            }
        } catch (Exception ex) {
            throw new InvalidOperationException("JWT认证配置失败", ex);
        }

        // =========== 认证相关服务 ===========
        // JWT和认证服务现已移至AuthModule中注册

        return services;
    }

    /// <summary>
    /// 注册业务模块服务
    /// </summary>
    private static IServiceCollection RegisterBusinessModules(this IServiceCollection services) {
        // 注册Users模块服务
        services.AddUsersModuleServices();

        // 注册Auth模块服务
        services.AddAuthModule();

        // 注册所有LYBT业务模块服务
        services.AddAllModules();

        // TODO: UltraThink v2.0 Refactor - 暂时禁用Formula服务注册，等待修复
        // 注册验方模块服务
        // services.AddScoped<LYBT.Shared.Interfaces.Services.IFormulaService, LYBT.Module.Formula.Services.FormulaService>();

        return services;
    }

    /// <summary>
    /// 注册API和文档服务
    /// </summary>
    private static IServiceCollection RegisterApiServices(this IServiceCollection services) {
        // API版本控制
        services.AddApiVersioning(opt => {
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

        // ProblemDetails 和异常处理服务
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Swagger文档
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c => {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
                Title = "凌隐宝堂中医诊所诊疗系统 API",
                Version = "v1",
                Description = "凌隐宝堂中医诊所诊疗系统API文档"
            });

            // 解决Schema ID冲突问题 - 生成真正唯一的Schema ID
            c.CustomSchemaIds(type => {
                if (type.IsGenericType) {
                    var genericDef = type.GetGenericTypeDefinition();
                    var genericTypeName = genericDef.FullName?.Split('`')[0]?.Replace(".", "") ?? genericDef.Name.Split('`')[0];

                    var genericArgs = type.GetGenericArguments()
                        .Select(arg => GetTypeSignature(arg))
                        .ToArray();

                    return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
                }

                return type.FullName?.Replace(".", "").Replace("+", "") ?? type.Name;
            });
        });

        // AutoMapper配置
        services.AddAutoMapperConfiguration();

        return services;

        // 本地辅助方法：生成类型签名
        static string GetTypeSignature(Type type) {
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
    }

    /// <summary>
    /// 注册控制器和JSON服务
    /// </summary>
    private static IServiceCollection RegisterControllerServices(this IServiceCollection services) {
        // 确保UTF-8编码支持
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        services.AddControllers().AddJsonOptions(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        });

        return services;
    }

    /// <summary>
    /// 注册日志和监控服务 - UltraThink简化版
    /// </summary>
    private static IServiceCollection RegisterLoggingAndMonitoringServices(
        this IServiceCollection services,
        ISimplifiedConfigurationService configService) {
        // =========== 统一日志管理 ===========
        // 注意：已简化为标准ILogger，无需额外配置

        // =========== 监控管理 ===========
        // 注意：IUnifiedMonitor的实现类需要在后续创建
        // services.AddScoped<LYBT.Infrastructure.Monitoring.IUnifiedMonitor, LYBT.Infrastructure.Monitoring.UnifiedMonitor>();

        return services;
    }
}
