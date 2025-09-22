using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

using LYBT.Infrastructure.Caching.Adapters;
using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Configuration.Extensions;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth;
using LYBT.Module.Users;
using LYBT.WebAPI.Configuration;
using LYBT.WebAPI.Extensions.ServiceCollection;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
        services.RegisterBusinessModules();

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
    private static IServiceCollection RegisterInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 数据库配置
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                              Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                              string.Empty;

        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                });

                var dbOptions = configuration.GetSection("DatabaseOptions").Get<DatabaseOptions>();
                options.EnableSensitiveDataLogging(dbOptions?.EnableSensitiveDataLogging ?? false);
                options.EnableDetailedErrors(dbOptions?.EnableDetailedErrors ?? false);
                options.EnableServiceProviderCaching();

                if (dbOptions?.CommandTimeout > 0)
                {
                    options.UseSqlServer(opt => opt.CommandTimeout(dbOptions.CommandTimeout));
                }
            });
        }

        // 内存缓存
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100_000;
            options.CompactionPercentage = 0.25;
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
        });

        services.AddSingleton<ICacheService, MemoryCacheAdapter>();

        // 选项绑定（IOptions）
        services.AddOptions<LYBT.Infrastructure.Configuration.Options.JwtOptions>()
            .Bind(configuration.GetSection(LYBT.Infrastructure.Configuration.Options.JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DefaultPasswordOptions>()
            .Bind(configuration.GetSection(DefaultPasswordOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SysAdminOptions>()
            .Bind(configuration.GetSection(SysAdminOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<LYBT.Infrastructure.Configuration.Options.UserOptions>()
            .Bind(configuration.GetSection("UserOptions"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<WebApiConfigurationOptions>()
            .Bind(configuration.GetSection(WebApiConfigurationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 常用服务
        services.AddHttpContextAccessor();
        services.AddScoped<DefaultPasswordService>();
        services.AddScoped<DatabaseInitializationService>();

        return services;
    }

    /// <summary>
    /// 注册认证与安全。
    /// </summary>
    private static IServiceCollection RegisterAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT 认证
        try
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                           configuration["JwtOptions:Secret"];

            if (string.IsNullOrEmpty(jwtSecret))
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("生产环境必须配置 JWT 密钥（JWT_SECRET 或配置项 JwtOptions:Secret）。");
                }

                jwtSecret = "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction";
            }

            if (!string.IsNullOrEmpty(jwtSecret))
            {
                var jwtSection = configuration.GetSection("JwtOptions");
                var issuer = jwtSection["Issuer"] ?? "LYBT";
                var audience = jwtSection["Audience"] ?? "LYBT-Client";
                var clockSkew = int.TryParse(jwtSection["ClockSkewSeconds"], out var skew) ? skew : 300;

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
    private static IServiceCollection RegisterBusinessModules(this IServiceCollection services)
    {
        services.AddAllModules();
        return services;
    }

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

        // Swagger（含 JWT）
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "凌隐宝堂中医诊所 API",
                Version = "v1",
                Description = "凌隐宝堂中医诊所 RESTful API 接口文档",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "技术支持",
                    Email = "support@lybt.com"
                },
                License = new Microsoft.OpenApi.Models.OpenApiLicense
                {
                    Name = "专有许可",
                    Url = new Uri("https://lybt.com/license")
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

            // 可选：包含 XML 注释
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
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
    private static IServiceCollection RegisterControllerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 确保 UTF-8 编码可用
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保持 PascalCase（与 DTO 匹配）
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // 忽略大小写
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.WriteIndented = false;
            // JSON 编码：默认安全，可通过配置开关放宽
            var unsafeEscaping = configuration.GetValue<bool>("WebApiOptions:Json:UnsafeRelaxedEscaping", false);
            options.JsonSerializerOptions.Encoder = unsafeEscaping
                ? JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                : JavaScriptEncoder.Default;
        });

        return services;
    }


    /// <summary>
    /// 配置速率限制（全局与登录端点）。
    /// </summary>
    private static IServiceCollection ConfigureRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 优先从 SecurityOptions.RateLimit 读取配置
        var securityOptions = new SecurityOptions();
        configuration.GetSection(SecurityOptions.SectionName).Bind(securityOptions);

        // 绑定速率限制配置（优先级：SecurityOptions.RateLimit > RateLimiting section > 默认值）
        var rateLimitingOptions = new RateLimitingOptions();

        // 先尝试从 RateLimiting 节读取
        if (configuration.GetSection(RateLimitingOptions.SectionName).Exists())
        {
            configuration.GetSection(RateLimitingOptions.SectionName).Bind(rateLimitingOptions);
        }
        // 如果SecurityOptions.RateLimit存在，则使用它（覆盖RateLimiting节）
        else if (securityOptions.RateLimit != null)
        {
            // 映射 SecurityOptions.RateLimit 到 RateLimitingOptions
            rateLimitingOptions.Enabled = securityOptions.RateLimit.Enabled;

            // 映射 General 限流规则
            rateLimitingOptions.Global.PermitLimit = securityOptions.RateLimit.General.RequestsPerMinute;
            rateLimitingOptions.Global.WindowSeconds = 60; // 固定为每分钟
            rateLimitingOptions.Global.QueueLimit = securityOptions.RateLimit.General.RequestsPerMinute / 2;

            // 映射 Authentication 限流规则
            rateLimitingOptions.Login.PermitLimit = securityOptions.RateLimit.Authentication.RequestsPerMinute;
            rateLimitingOptions.Login.WindowSeconds = 60;
            rateLimitingOptions.Login.QueueLimit = securityOptions.RateLimit.Authentication.RequestsPerMinute;

            // 映射 API 限流规则
            rateLimitingOptions.Api.UserPermitLimit = securityOptions.RateLimit.General.RequestsPerMinute;
            rateLimitingOptions.Api.AdminPermitLimit = securityOptions.RateLimit.ApiKey.RequestsPerMinute;
            rateLimitingOptions.Api.WindowSeconds = 60;
            rateLimitingOptions.Api.QueueLimit = securityOptions.RateLimit.General.RequestsPerMinute / 2;

            // 映射白名单IP（如果SecurityOptions中有相关配置）
            if (securityOptions.Environment?.TrustedProxies != null)
            {
                rateLimitingOptions.WhitelistedIPs = securityOptions.Environment.TrustedProxies;
            }
        }
        // 否则使用默认值，生产环境更严格
        else
        {
            if (environment.IsProduction())
            {
                rateLimitingOptions.Global.PermitLimit = 60;
                rateLimitingOptions.Login.PermitLimit = 5;
                rateLimitingOptions.Api.UserPermitLimit = 60;
                rateLimitingOptions.Api.AdminPermitLimit = 200;
            }
        }

        // 如果禁用了速率限制，直接返回
        if (!rateLimitingOptions.Enabled)
        {
            return services;
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 全局速率限制
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userKey = context.User?.Identity?.IsAuthenticated == true
                    ? (context.User.Identity?.Name ?? "auth-anon")
                    : (context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip");

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userKey,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingOptions.Global.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.Global.WindowSeconds),
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = rateLimitingOptions.Global.QueueLimit
                    });
            });

            // 登录端点速率限制
            options.AddPolicy("Login", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

                // 检查是否为白名单IP
                var isWhitelisted = IsWhitelistedIp(ip, rateLimitingOptions.WhitelistedIPs) ||
                                   IsPrivateIp(ip);

                var limit = isWhitelisted
                    ? rateLimitingOptions.Login.InternalPermitLimit
                    : rateLimitingOptions.Login.PermitLimit;

                var queue = isWhitelisted
                    ? rateLimitingOptions.Login.InternalQueueLimit
                    : rateLimitingOptions.Login.QueueLimit;

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ip,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.Login.WindowSeconds),
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = queue
                    });
            });

            // API端点速率限制（基于角色）
            options.AddPolicy("Api", httpContext =>
            {
                var user = httpContext.User;
                var isAdmin = user?.IsInRole("Admin") ?? false;
                var userKey = user?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                var limit = isAdmin
                    ? rateLimitingOptions.Api.AdminPermitLimit
                    : rateLimitingOptions.Api.UserPermitLimit;

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userKey,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.Api.WindowSeconds),
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = rateLimitingOptions.Api.QueueLimit
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
