using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Logging;
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
public static class UnifiedServiceRegistration
{
    /// <summary>
    /// 注册所有应用服务（统一入口）
    /// </summary>
    public static IServiceCollection RegisterAllApplicationServices(
        this IServiceCollection services, 
        IConfiguration configuration, 
        IWebHostEnvironment environment)
    {
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
        IConfiguration configuration)
    {
        // =========== 统一配置管理系统 ===========
        services.AddUnifiedConfiguration(configuration);
        services.AddScoped<ISecretManager, SecretManager>();
        services.AddScoped<IEnvironmentManager, EnvironmentManager>();
        services.AddScoped<IEnvironmentVariableReplacer, EnvironmentVariableReplacer>();

        // =========== 统一数据库上下文 ===========
        var configManager = services.BuildServiceProvider().GetRequiredService<LYBT.Infrastructure.Configuration.IConfigurationManager>();
        var connectionString = configManager.GetConnectionString();
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                });
                
                var dbOptions = configManager.GetSection<LYBT.Infrastructure.Configuration.Options.DatabaseOptions>("DatabaseOptions");
                options.EnableSensitiveDataLogging(dbOptions?.EnableSensitiveDataLogging ?? false);
                options.EnableDetailedErrors(dbOptions?.EnableDetailedErrors ?? false);
                options.EnableServiceProviderCaching();
                
                if (dbOptions?.CommandTimeout > 0)
                {
                    options.UseSqlServer(opt => opt.CommandTimeout(dbOptions.CommandTimeout));
                }
            });
        }

        // =========== 缓存服务 ===========
        services.AddMemoryCache(options =>
        {
            var cacheOptions = configManager.GetSection<LYBT.Infrastructure.Configuration.Options.CacheOptions>("CacheOptions");
            if (cacheOptions?.MemoryCache != null)
            {
                options.SizeLimit = cacheOptions.MemoryCache.SizeLimit;
                options.CompactionPercentage = cacheOptions.MemoryCache.CompactionPercentage;
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(cacheOptions.MemoryCache.ExpirationScanFrequency);
            }
        });
        services.AddSingleton<ICacheService, CacheService>();

        // =========== 配置选项绑定（支持环境变量覆盖）===========
        // 注册SysAdminOptions，优先使用环境变量
        services.Configure<LYBT.Infrastructure.Configuration.Options.SysAdminOptions>(options =>
        {
            // 从环境变量读取，如果不存在则从配置文件读取
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD")
                ?? configuration["SysAdminOptions:DefaultPassword"];
            if (!string.IsNullOrEmpty(adminPassword))
            {
                options.DefaultPassword = adminPassword;
            }
            configuration.GetSection("SysAdminOptions").Bind(options);
        });

        // 注册UserOptions，优先使用环境变量
        services.Configure<LYBT.Module.Users.UserOptions>(options =>
        {
            // 从环境变量读取，如果不存在则从配置文件读取
            var userPassword = Environment.GetEnvironmentVariable("USER_DEFAULT_PASSWORD")
                ?? configuration["UserOptions:DefaultUserPassword"];
            if (!string.IsNullOrEmpty(userPassword))
            {
                options.DefaultUserPassword = userPassword;
            }
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
        
        // =========== 性能优化服务 ===========
        services.RegisterPerformanceServices(configManager);
        
        // =========== 日志和监控服务 ===========
        services.RegisterLoggingAndMonitoringServices(configManager);
        
        // =========== 数据库初始化服务 ===========
        services.AddScoped<LYBT.Infrastructure.Database.DatabaseInitializationService>();

        return services;
    }

    /// <summary>
    /// 注册性能优化服务
    /// </summary>
    private static IServiceCollection RegisterPerformanceServices(
        this IServiceCollection services, 
        LYBT.Infrastructure.Configuration.IConfigurationManager configManager)
    {
        // =========== 统一缓存管理 ===========
        var cacheOptions = configManager.GetSection<LYBT.Infrastructure.Configuration.Options.CacheOptions>("CacheOptions");
        var unifiedCacheOptions = new LYBT.Infrastructure.Performance.Cache.CacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(cacheOptions?.DefaultExpiryMinutes ?? 30),
            EnableCompression = cacheOptions?.Performance?.EnableCompression ?? true,
            CompressionThreshold = cacheOptions?.Performance?.CompressionThreshold ?? 1024
        };
        
        services.AddSingleton(unifiedCacheOptions);
        services.AddScoped<LYBT.Infrastructure.Performance.Cache.IUnifiedCacheManager, LYBT.Infrastructure.Performance.Cache.UnifiedCacheManager>();

        // =========== 数据库性能优化 ===========
        // UltraThink v2.0: 禁用复杂数据库性能优化 - 20人以下小诊所不需要复杂的数据库性能监控和优化
        // services.AddScoped<LYBT.Infrastructure.Performance.Database.IUnifiedDatabaseOptimizer, LYBT.Infrastructure.Performance.Database.UnifiedDatabaseOptimizer>();

        // =========== 异步处理管理 ===========
        services.AddSingleton<LYBT.Infrastructure.Performance.Async.IUnifiedAsyncProcessor, LYBT.Infrastructure.Performance.Async.UnifiedAsyncProcessor>();
        services.AddHostedService(provider => 
            (LYBT.Infrastructure.Performance.Async.UnifiedAsyncProcessor)provider.GetRequiredService<LYBT.Infrastructure.Performance.Async.IUnifiedAsyncProcessor>());

        return services;
    }

    /// <summary>
    /// 注册认证和安全服务
    /// </summary>
    private static IServiceCollection RegisterAuthenticationServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // =========== JWT认证配置（使用统一配置管理）===========
        var serviceProvider = services.BuildServiceProvider();
        var configManager = serviceProvider.GetRequiredService<LYBT.Infrastructure.Configuration.IConfigurationManager>();
        var secretManager = serviceProvider.GetRequiredService<ISecretManager>();
        
        try
        {
            // 获取JWT配置
            var jwtSection = configuration.GetSection("JwtOptions");
            var jwtOptions = jwtSection.Get<LYBT.Infrastructure.Configuration.Options.JwtOptions>() 
                ?? new LYBT.Infrastructure.Configuration.Options.JwtOptions();
            
            // 从秘钥管理器获取JWT密钥
            if (string.IsNullOrEmpty(jwtOptions.Secret) || jwtOptions.Secret.Contains("${"))
            {
                var jwtSecret = secretManager.GetSecret("JWT_SECRET");
                if (!string.IsNullOrEmpty(jwtSecret))
                {
                    jwtOptions.Secret = jwtSecret;
                }
                else
                {
                    // 在开发环境中，如果没有找到JWT_SECRET，使用配置文件中的值
                    if (configManager.IsDevelopment && !string.IsNullOrEmpty(jwtOptions.Secret))
                    {
                        // 使用配置文件中的值
                    }
                    else
                    {
                        throw new InvalidOperationException("JWT密钥配置错误：无法获取有效的JWT密钥");
                    }
                }
            }
            
            // 注册处理过的JwtOptions到DI容器
            services.AddSingleton<Microsoft.Extensions.Options.IOptions<LYBT.Infrastructure.Configuration.Options.JwtOptions>>(
                new Microsoft.Extensions.Options.OptionsWrapper<LYBT.Infrastructure.Configuration.Options.JwtOptions>(jwtOptions));
            
            if (!string.IsNullOrEmpty(jwtOptions.Secret))
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
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
            else
            {
                throw new InvalidOperationException("JWT密钥为空，无法配置认证");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("JWT认证配置失败", ex);
        }

        // =========== 认证相关服务 ===========
        // JWT和认证服务现已移至AuthModule中注册

        return services;
    }

    /// <summary>
    /// 注册业务模块服务
    /// </summary>
    private static IServiceCollection RegisterBusinessModules(this IServiceCollection services)
    {
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
    private static IServiceCollection RegisterApiServices(this IServiceCollection services)
    {
        // API版本控制
        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                new Asp.Versioning.UrlSegmentApiVersionReader(),
                new Asp.Versioning.QueryStringApiVersionReader("version"),
                new Asp.Versioning.HeaderApiVersionReader("X-Version"));
        }).AddApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        // ProblemDetails 和异常处理服务
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Swagger文档
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "凌隐宝堂中医诊所诊疗系统 API",
                Version = "v1",
                Description = "凌隐宝堂中医诊所诊疗系统API文档"
            });

            // 解决Schema ID冲突问题 - 生成真正唯一的Schema ID
            c.CustomSchemaIds(type =>
            {
                if (type.IsGenericType)
                {
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
        static string GetTypeSignature(Type type)
        {
            if (type.IsGenericType)
            {
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
    private static IServiceCollection RegisterControllerServices(this IServiceCollection services)
    {
        // 确保UTF-8编码支持
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        services.AddControllers().AddJsonOptions(options =>
        {
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
    /// 注册日志和监控服务
    /// </summary>
    private static IServiceCollection RegisterLoggingAndMonitoringServices(
        this IServiceCollection services,
        LYBT.Infrastructure.Configuration.IConfigurationManager configManager)
    {
        // =========== 统一日志管理 ===========
        // 注意：已简化为标准ILogger，无需额外配置

        // =========== 监控管理 ===========
        // 注意：IUnifiedMonitor的实现类需要在后续创建
        // services.AddScoped<LYBT.Infrastructure.Monitoring.IUnifiedMonitor, LYBT.Infrastructure.Monitoring.UnifiedMonitor>();

        return services;
    }
}