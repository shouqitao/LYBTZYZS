using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

// using LYBT.Infrastructure.Configuration; // Removed - SimplifiedConfigurationService eliminated
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Data;

// using LYBT.Infrastructure.Security; // Removed - obsolete security components eliminated
using LYBT.Module.Auth;
using LYBT.Module.Users;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
        // =========== 直接使用 IConfiguration - 消除配置服务套娃 ===========
        // 直接使用 .NET 内置 IConfiguration，避免额外的包装层

        // =========== 统一数据库上下文 ===========
        var connectionString = GetConnectionString(configuration);

        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                });

                var dbOptions = GetConfigurationSection<DatabaseOptions>(configuration, "DatabaseOptions");
                options.EnableSensitiveDataLogging(dbOptions?.EnableSensitiveDataLogging ?? false);
                options.EnableDetailedErrors(dbOptions?.EnableDetailedErrors ?? false);
                options.EnableServiceProviderCaching();

                if (dbOptions?.CommandTimeout > 0)
                {
                    options.UseSqlServer(opt => opt.CommandTimeout(dbOptions.CommandTimeout));
                }

                // Epic 05-P0-03: 敏感数据拦截器已移除 (SensitiveDataInterceptor marked as Obsolete)
                // 小型诊所不需要复杂的自动数据加密，使用手动加密更合适
            });
        }

        // =========== 缓存服务 - UltraThink简化版 ===========
        services.AddMemoryCache(options =>
        {
            // 使用默认配置，移除已删除的CacheOptions依赖
            options.SizeLimit = 100_000; // 默认缓存项目数量
            options.CompactionPercentage = 0.25; // 内存压力时清理25%
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(1); // 每分钟扫描过期项
        });

        // =========== 配置选项绑定 - 直接使用 IOptions<T> 模式 ===========
        // 消除配置服务套娃，直接绑定配置并支持环境变量覆盖
        services.Configure<SysAdminOptions>(options =>
        {
            var adminPassword = GetAdminPassword(configuration);
            options.DefaultPassword = adminPassword;
            configuration.GetSection("SysAdminOptions").Bind(options);
        });

        services.Configure<UserOptions>(options =>
        {
            var userPassword = GetUserDefaultPassword(configuration);
            options.DefaultUserPassword = userPassword;
            configuration.GetSection("UserOptions").Bind(options);
        });

        // =========== 配置验证服务 - DT-012优化 ===========
        // 为小型诊所部署启用启动时配置验证，防止配置错误导致的问题
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // CacheOptions已删除，使用内存缓存默认配置

        // =========== 安全配置服务 - 最小有效实现 ===========

        // 保留HTTP上下文访问器（其他服务需要）
        services.AddHttpContextAccessor();

        // ❌ 已移除过时的安全组件：
        // - IDataEncryptionService/DataEncryptionService (标记为Obsolete，小型诊所用不到自动加密)
        // - ISecurityAuditService/SecurityAuditService (标记为Obsolete，小型诊所用基础日志即可)
        // - SensitiveDataInterceptor (标记为Obsolete，复杂度过高)
        // - SensitiveDataQueryInterceptor (标记为Obsolete，自动解密复杂度过高)

        // =========== 性能优化服务 - UltraThink简化版 ===========
        services.RegisterPerformanceServices();

        // =========== 日志和监控服务 - UltraThink简化版 ===========
        services.RegisterLoggingAndMonitoringServices();

        // =========== 数据库初始化服务 ===========
        services.AddScoped<DatabaseInitializationService>();

        return services;
    }

    /// <summary>
    /// 注册性能优化服务 - UltraThink简化版本
    /// 移除过度设计的性能组件，使用.NET内置服务
    /// </summary>
    private static IServiceCollection RegisterPerformanceServices(this IServiceCollection services)
    {
        // =========== 简化缓存管理 ===========
        // UltraThink简化：使用内置IMemoryCache替代复杂的UnifiedCacheManager
        services.AddMemoryCache(options =>
        {
            // 使用默认配置，避免复杂的配置选项
            options.SizeLimit = 1000; // 简化：使用固定的缓存大小限制
        });
        return services;
    }

    /// <summary>
    /// 注册认证和安全服务
    /// </summary>
    private static IServiceCollection RegisterAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // =========== JWT认证配置 - 直接使用IConfiguration ===========
        try
        {
            // 获取JWT配置
            var jwtSection = configuration.GetSection("JwtOptions");
            var jwtOptions = jwtSection.Get<JwtOptions>()
                ?? new JwtOptions();

            // 直接使用配置获取JWT密钥
            jwtOptions.Secret = GetJwtSecret(configuration);

            // 注册处理过的JwtOptions到DI容器
            services.AddSingleton<Microsoft.Extensions.Options.IOptions<JwtOptions>>(
                new Microsoft.Extensions.Options.OptionsWrapper<JwtOptions>(jwtOptions));

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

        return services;
    }

    /// <summary>
    /// 注册API和文档服务
    /// </summary>
    private static IServiceCollection RegisterApiServices(this IServiceCollection services)
    {
        // ✅ 简化API版本控制 - 控制器中的 [ApiVersion("1")] 标注已足够

        // ProblemDetails 和异常处理服务
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Swagger文档配置（集成JWT认证支持）
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "凌隐宝堂中医诊所诊疗系统 API",
                Version = "v1",
                Description = "凌隐宝堂中医诊所诊疗系统API文档",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "技术支持",
                    Email = "support@lybt.com"
                },
                License = new Microsoft.OpenApi.Models.OpenApiLicense
                {
                    Name = "专有软件许可",
                    Url = new Uri("https://lybt.com/license")
                }
            });

            // JWT Bearer认证配置
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // 包含XML注释
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // 解决Schema ID冲突问题 - 生成真正唯一的Schema ID
            c.CustomSchemaIds(type =>
            {
                if (type.IsGenericType)
                {
                    var genericDef = type.GetGenericTypeDefinition();
                    var genericTypeName = genericDef.FullName?.Split('`')[0]?.Replace(".", string.Empty) ?? genericDef.Name.Split('`')[0];

                    var genericArgs = type.GetGenericArguments()
                        .Select(arg => GetTypeSignature(arg))
                        .ToArray();

                    return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
                }

                return type.FullName?.Replace(".", string.Empty).Replace("+", string.Empty) ?? type.Name;
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
            options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        });

        return services;
    }

    /// <summary>
    /// 注册日志和监控服务 - UltraThink简化版
    /// </summary>
    private static IServiceCollection RegisterLoggingAndMonitoringServices(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>
    /// 获取数据库连接字符串 - 直接使用IConfiguration
    /// 优先级: CONNECTION_STRING环境变量 -> 配置文件
    /// </summary>
    private static string GetConnectionString(IConfiguration configuration, string name = "DefaultConnection")
    {
        // 优先使用环境变量
        var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            return envConnectionString;
        }

        // 使用配置文件
        return configuration.GetConnectionString(name) ?? string.Empty;
    }

    /// <summary>
    /// 获取配置节 - 直接使用IConfiguration
    /// </summary>
    private static T GetConfigurationSection<T>(IConfiguration configuration, string sectionName) where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        var config = new T();
        section.Bind(config);
        return config;
    }

    /// <summary>
    /// 获取管理员密码 - 直接使用IConfiguration
    /// 优先级: ADMIN_DEFAULT_PASSWORD环境变量 -> 配置文件
    /// </summary>
    private static string GetAdminPassword(IConfiguration configuration)
    {
        // 优先使用环境变量
        var envPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
        {
            return envPassword;
        }

        // 使用配置文件
        var configPassword = configuration["SysAdminOptions:DefaultPassword"];
        if (!string.IsNullOrEmpty(configPassword))
        {
            return configPassword;
        }

        throw new InvalidOperationException("管理员密码未配置：请设置ADMIN_DEFAULT_PASSWORD环境变量或配置文件中的SysAdminOptions:DefaultPassword");
    }

    /// <summary>
    /// 获取用户默认密码 - 直接使用IConfiguration
    /// 优先级: USER_DEFAULT_PASSWORD环境变量 -> 配置文件
    /// </summary>
    private static string GetUserDefaultPassword(IConfiguration configuration)
    {
        // 优先使用环境变量
        var envPassword = Environment.GetEnvironmentVariable("USER_DEFAULT_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
        {
            return envPassword;
        }

        // 使用配置文件
        var configPassword = configuration["UserOptions:DefaultUserPassword"];
        if (!string.IsNullOrEmpty(configPassword))
        {
            return configPassword;
        }

        throw new InvalidOperationException("用户默认密码未配置：请设置USER_DEFAULT_PASSWORD环境变量或配置文件中的UserOptions:DefaultUserPassword");
    }

    /// <summary>
    /// 获取JWT密钥 - 直接使用IConfiguration
    /// 优先级: JWT_SECRET环境变量 -> 配置文件 -> 开发环境默认值
    /// </summary>
    private static string GetJwtSecret(IConfiguration configuration)
    {
        // 优先使用环境变量
        var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (!string.IsNullOrEmpty(envSecret))
        {
            return envSecret;
        }

        // 使用配置文件
        var configSecret = configuration["JwtOptions:Secret"];
        if (!string.IsNullOrEmpty(configSecret) && !configSecret.Contains("${"))
        {
            return configSecret;
        }

        // 开发环境允许使用默认值
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return "UltraThink-LYBT-Development-Secret-Key-2025-09-02-Very-Long-Secret-For-JWT-Signing";
        }

        throw new InvalidOperationException("JWT密钥未配置：请设置JWT_SECRET环境变量或配置文件中的JwtOptions:Secret");
    }
}
