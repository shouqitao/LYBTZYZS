using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

using LYBT.Core.Infrastructure.Caching.Adapters;
using LYBT.Core.Infrastructure.Caching.Interfaces;
using LYBT.Core.Infrastructure.Configuration.Options;
using LYBT.WebAPI.Services;
using LYBT.Core.Infrastructure.Configuration.Services;
using LYBT.Core.Infrastructure.Configuration.Extensions;
using LYBT.Core.Infrastructure.Data;
using LYBT.Module.Auth;
using LYBT.Module.Users;
using LYBT.Module.Consultation;
using LYBT.Module.Herbs;
using LYBT.Module.Prescriptions;
using LYBT.Module.Patients;
using LYBT.Module.Formula;
using LYBT.Module.MedicalCase;
using LYBT.WebAPI.Configuration;
using LYBT.WebAPI.Extensions.ServiceCollection;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using AutoMapper;


namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 统一服务注册（UltraThink 装配体系）
/// 将各项服务装配逻辑集中一处，提升一致性与可维护性。
/// </summary>
public static class UnifiedServiceRegistration
{
    /// <summary>
    /// 注册应用服务（统一入口）。
    /// </summary>
    public static IServiceCollection RegisterAllApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 1）基础设施
        services.RegisterInfrastructureServices(configuration);

        // 2）认证与安全
        services.RegisterAuthenticationServices(configuration);

        // 3）业务模块
        services.RegisterBusinessModules(configuration);

        // 4）API 文档
        services.RegisterApiServices();

        // 5）控制器与 JSON
        services.RegisterControllerServices(configuration);


        // 7）性能优化（服务注册）
        services.ConfigurePerformanceOptimizations(configuration);

        // 8）API 版本管理
        services.ConfigureApiVersioning();

        // 9）速率限制（全局 + 登录）
        services.ConfigureRateLimiting(configuration, environment);

        // 10）安全服务（数据保护、密钥管理、密钥旋转）
        services.AddSecurityServices(configuration, environment);

        // 11）环境感知配置校验（生产强校验）- 使用Infrastructure统一实现
        services.AddEnvironmentAwareValidation(environment);

        return services;
    }

    /// <summary>
    /// 注册基础设施服务。
    /// </summary>
    /// <summary>
    /// 注册基础设施服务。
    /// </summary>
    private static IServiceCollection RegisterInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // =========== UltraThink Phase 2：统一配置管理 ===========
        // 注册新的统一配置系统，同时保持向后兼容
        services.AddLybtConfiguration(configuration);

        // 验证配置（生产环境强制验证）
        var validationResult = configuration.ValidateLybtConfiguration();
        if (!validationResult.IsValid)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var errors = string.Join(Environment.NewLine, validationResult.Errors);
            
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"生产环境配置验证失败：{Environment.NewLine}{errors}");
            }
            else
            {
                // 开发环境记录警告但继续运行
                Console.WriteLine($"配置验证警告：{Environment.NewLine}{errors}");
            }
        }

        // 数据库配置 - 从统一配置读取
        var lybtOptions = configuration.GetLybtOptions();
        var connectionString = lybtOptions.Infrastructure.Database.ConnectionString ??
                              configuration.GetConnectionString("DefaultConnection") ??
                              Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                              string.Empty;

        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var sqlOptions = options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(
                        lybtOptions.Infrastructure.Database.RetryPolicy.MaxRetryCount,
                        TimeSpan.FromMilliseconds(lybtOptions.Infrastructure.Database.RetryPolicy.MaxDelayMs),
                        null);
                });

                // 使用统一配置的监控设置
                options.EnableSensitiveDataLogging(lybtOptions.Infrastructure.Database.Monitoring.LogAllQueries);
                options.EnableDetailedErrors(true); // 开发环境启用详细错误
                options.EnableServiceProviderCaching();

                // 设置命令超时
                if (lybtOptions.Infrastructure.Database.ConnectionPool.CommandTimeoutSeconds > 0)
                {
                    options.UseSqlServer(opt => opt.CommandTimeout(lybtOptions.Infrastructure.Database.ConnectionPool.CommandTimeoutSeconds));
                }
            });
        }

        // 缓存配置 - 使用统一配置
        if (lybtOptions.Infrastructure.Cache.MemoryCache.SizeLimit <= 0)
        {
            services.AddSingleton<ICacheService>(new NullCacheService());
            services.AddMemoryCache(); // 添加基础的MemoryCache，即使禁用也需要
        }
        else
        {
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = lybtOptions.Infrastructure.Cache.MemoryCache.SizeLimit;
                options.CompactionPercentage = lybtOptions.Infrastructure.Cache.MemoryCache.CompactionPercentage;
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(lybtOptions.Infrastructure.Cache.MemoryCache.ExpirationScanFrequencySeconds);
            });

            services.AddSingleton<ICacheService, MemoryCacheAdapter>();

            // 缓存诊断服务（Phase 3缓存治理）
            // TODO: 实现 CacheDiagnosticsService 后启用
            // services.AddSingleton<ICacheDiagnosticsService, CacheDiagnosticsService>();
            // services.AddHostedService<CacheHealthBackgroundService>();
        }

        // =========== 保持向后兼容：注册传统配置选项 ===========
        // 注意：这些配置选项已通过 AddLybtConfiguration 自动映射和注册
        // 这里仅显式验证关键配置选项以确保启动时验证

        // 验证 JWT 配置
        if (string.IsNullOrEmpty(lybtOptions.Authentication.Jwt.SecretKey))
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("生产环境必须配置 JWT 密钥。");
            }
        }

        // 验证数据库连接
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("必须配置数据库连接字符串。");
        }

        // 常用服务
        services.AddHttpContextAccessor();
        services.AddScoped<DefaultPasswordService>();
        services.AddScoped<DatabaseInitializationService>();

        return services;
    }

    /// <summary>
    /// 注册认证与安全。
    /// </summary>
    /// <summary>
    /// 注册认证与安全。
    /// </summary>
    private static IServiceCollection RegisterAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // =========== UltraThink Phase 2：使用统一配置 ===========
        var lybtOptions = configuration.GetLybtOptions();
        
        // JWT 认证 - 从统一配置读取
        try
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                           lybtOptions.Authentication.Jwt.SecretKey;

            if (string.IsNullOrEmpty(jwtSecret))
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("生产环境必须配置 JWT 密钥（JWT_SECRET 或 Lybt:Authentication:Jwt:SecretKey）。");
                }

                jwtSecret = "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction";
            }

            if (!string.IsNullOrEmpty(jwtSecret))
            {
                var issuer = lybtOptions.Authentication.Jwt.Issuer;
                var audience = lybtOptions.Authentication.Jwt.Audience;
                var clockSkew = 300; // 固定5分钟时钟偏差

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
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ClockSkew = TimeSpan.FromSeconds(clockSkew)
                    };
                });
            }
            else
            {
                throw new InvalidOperationException("JWT 密钥为空，无法配置认证。");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("配置 JWT 认证失败", ex);
        }

        // 配置授权策略
        services.AddAuthorization(options =>
        {
            // 设置默认策略 - 要求所有端点默认需要认证
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // 设置全局回退策略 - 要求所有用户必须认证（未标注任何授权属性的端点）
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // 定义基于角色的策略
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Admin"));

            options.AddPolicy("DoctorOrAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Doctor", "Admin"));

            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }

    /// <summary>
    /// 注册业务模块。
    /// </summary>
    private static IServiceCollection RegisterBusinessModules(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册各业务模块
        services.AddAuthModule();
        services.AddUsersModuleServices();
        services.AddConsultationModule();
        services.AddHerbsModule();
        services.AddPrescriptionsModule();
        services.AddPatientsModuleServices();
        services.AddFormulaModule();
        services.AddMedicalCaseModule();
        return services;
    }

    /// <summary>
    /// 注册 API 文档（Swagger）与统一异常处理。
    /// </summary>
    /// <summary>
    /// 注册 API 文档（Swagger）与统一异常处理。
    /// </summary>
    private static IServiceCollection RegisterApiServices(this IServiceCollection services)
    {
        // API 版本管理
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                new Asp.Versioning.QueryStringApiVersionReader("version"),
                new Asp.Versioning.HeaderApiVersionReader("X-Version"),
                new Asp.Versioning.UrlSegmentApiVersionReader());
        }).AddMvc().AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // ProblemDetails + 全局异常处理器
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Swagger（含 JWT）- 从服务提供者获取配置
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // 由于这里无法直接访问配置，使用服务提供者在运行时获取配置
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var lybtOptions = configuration.GetLybtOptions();
            var swaggerConfig = lybtOptions.Application.WebApi.Swagger;
            
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = swaggerConfig.Title,
                Version = "v1",
                Description = swaggerConfig.Description,
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = swaggerConfig.ContactName,
                    Email = swaggerConfig.ContactEmail,
                    Url = !string.IsNullOrEmpty(swaggerConfig.ContactUrl) ? new Uri(swaggerConfig.ContactUrl) : null
                },
                License = new Microsoft.OpenApi.Models.OpenApiLicense
                {
                    Name = swaggerConfig.LicenseName,
                    Url = !string.IsNullOrEmpty(swaggerConfig.LicenseUrl) ? new Uri(swaggerConfig.LicenseUrl) : null
                }
            });

            // JWT Bearer security definition
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

            // XML 注释 - 使用统一配置控制
            if (swaggerConfig.EnableXmlComments)
            {
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            }

            // 避免 Schema ID 冲突
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

        // AutoMapper 配置
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("LYBT.") == true)
            .ToArray();
        services.AddAutoMapper(cfg => cfg.AddMaps(assemblies), assemblies);

        return services;

        // 生成 Schema ID 的帮助方法
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
    /// 注册控制器与 JSON 选项。
    /// </summary>
    /// <summary>
    /// 注册控制器与 JSON 选项。
    /// </summary>
    private static IServiceCollection RegisterControllerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // =========== UltraThink Phase 2：使用统一配置 ===========
        var lybtOptions = configuration.GetLybtOptions();
        var jsonConfig = lybtOptions.Application.WebApi.Json;

        // 确保 UTF-8 编码可用
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        services.AddControllers().AddJsonOptions(options =>
        {
            // 使用统一配置的属性命名策略
            options.JsonSerializerOptions.PropertyNamingPolicy = jsonConfig.PropertyNamingPolicy switch
            {
                "CamelCase" => System.Text.Json.JsonNamingPolicy.CamelCase,
                "SnakeCaseLower" => System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                "SnakeCaseUpper" => System.Text.Json.JsonNamingPolicy.SnakeCaseUpper,
                "KebabCaseLower" => System.Text.Json.JsonNamingPolicy.KebabCaseLower,
                "KebabCaseUpper" => System.Text.Json.JsonNamingPolicy.KebabCaseUpper,
                _ => null // PascalCase (默认)
            };
            
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // 忽略大小写
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.IgnoreReadOnlyProperties = jsonConfig.IgnoreReadOnlyProperties;
            options.JsonSerializerOptions.AllowTrailingCommas = jsonConfig.AllowTrailingCommas;
            
            // JSON 编码：使用统一配置的设置
            options.JsonSerializerOptions.Encoder = jsonConfig.UnsafeRelaxedEscaping
                ? JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                : JavaScriptEncoder.Default;
        });

        return services;
    }


    /// <summary>
    /// 配置速率限制（全局与登录端点）。
    /// </summary>
    /// <summary>
    /// 配置速率限制（全局与登录端点）。
    /// </summary>
    private static IServiceCollection ConfigureRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // =========== UltraThink Phase 2：使用统一配置 ===========
        var lybtOptions = configuration.GetLybtOptions();
        var rateLimitingConfig = lybtOptions.Security.RateLimiting;

        // 如果禁用了速率限制，直接返回
        if (!rateLimitingConfig.Enabled)
        {
            return services;
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 全局速率限制 - 使用统一配置
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userKey = context.User?.Identity?.IsAuthenticated == true
                    ? (context.User.Identity?.Name ?? "auth-anon")
                    : (context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip");

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userKey,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.GlobalLimit.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.GlobalLimit.WindowSeconds),
                        QueueProcessingOrder = rateLimitingConfig.GlobalLimit.QueueProcessingOrder == QueueProcessingOrder.OldestFirst 
                            ? System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst 
                            : System.Threading.RateLimiting.QueueProcessingOrder.NewestFirst,
                        QueueLimit = 0 // 不使用队列，直接拒绝
                    });
            });

            // 登录端点速率限制 - 使用统一配置
            options.AddPolicy("Login", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

                // 检查是否为白名单IP
                var isWhitelisted = IsWhitelistedIp(ip, lybtOptions.Security.IpSecurity.AllowedIpAddresses) ||
                                   IsPrivateIp(ip);

                // 如果是白名单IP，使用更宽松的限制
                var limit = isWhitelisted 
                    ? rateLimitingConfig.LoginLimit.PermitLimit * 2  // 白名单IP双倍限制
                    : rateLimitingConfig.LoginLimit.PermitLimit;

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ip,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.LoginLimit.WindowSeconds),
                        QueueProcessingOrder = rateLimitingConfig.LoginLimit.QueueProcessingOrder == QueueProcessingOrder.OldestFirst 
                            ? System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst 
                            : System.Threading.RateLimiting.QueueProcessingOrder.NewestFirst,
                        QueueLimit = 0
                    });
            });

            // API端点速率限制 - 使用统一配置
            options.AddPolicy("Api", httpContext =>
            {
                var user = httpContext.User;
                var isAdmin = user?.IsInRole("Admin") ?? false;
                var userKey = user?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                var limit = isAdmin
                    ? rateLimitingConfig.ApiLimit.PermitLimit * 2  // 管理员双倍限制
                    : rateLimitingConfig.ApiLimit.PermitLimit;

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userKey,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.ApiLimit.WindowSeconds),
                        QueueProcessingOrder = rateLimitingConfig.ApiLimit.QueueProcessingOrder == QueueProcessingOrder.OldestFirst 
                            ? System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst 
                            : System.Threading.RateLimiting.QueueProcessingOrder.NewestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// 检查是否为私有IP地址
    /// </summary>
    private static bool IsPrivateIp(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        if (ip.StartsWith("127.")) return true;
        if (ip.Equals("::1")) return true;
        if (ip.StartsWith("10.")) return true;
        if (ip.StartsWith("192.168.")) return true;
        if (ip.StartsWith("172."))
        {
            var parts = ip.Split('.');
            if (parts.Length > 1 && int.TryParse(parts[1], out var b) && b >= 16 && b <= 31)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检查是否为白名单IP
    /// </summary>
    private static bool IsWhitelistedIp(string ip, List<string> whitelistedIPs)
    {
        if (string.IsNullOrEmpty(ip) || whitelistedIPs == null || whitelistedIPs.Count == 0)
            return false;

        return whitelistedIPs.Contains(ip);
    }
}
