using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using LYBT.Core.Infrastructure.Configuration.Extensions;
using LYBT.Core.Infrastructure.Configuration.Options;
using LYBT.Module.Auth;
using LYBT.Module.Consultation;
using LYBT.Module.Formula;
using LYBT.Module.Herbs;
using LYBT.Module.MedicalCase;
using LYBT.Module.Patients;
using LYBT.Module.Prescriptions;
using LYBT.Module.Users;
using LYBT.WebAPI.Extensions.ServiceCollection;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        services.RegisterBusinessModules(configuration);

        // 4）API 文档
        services.RegisterApiServices();

        // 5）控制器与 JSON
        services.RegisterControllerServices(configuration);


        // 7）性能优化（服务注册）
        services.ConfigurePerformanceOptimizations(configuration);

        // 8）API 版本管理
        services.ConfigureApiVersioning();

        // 9）速率限制 - 轻量级登录保护（必要的安全基线）
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

        // 缓存配置 - 使用统一配置
        services.AddMemoryCache(); // 总是添加基础的MemoryCache

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

        if (lybtOptions.Infrastructure.Cache.MemoryCache.SizeLimit <= 0)
        {
            // 使用 NullCacheService
            services.AddSingleton<LYBT.Core.Infrastructure.Caching.Interfaces.ICacheService, LYBT.Core.Infrastructure.Caching.Adapters.NullCacheService>();
            // services.AddSingleton<LYBT.Infrastructure.Caching.Interfaces.ICacheService, LYBT.Core.Infrastructure.Caching.Adapters.NullCacheService>(); // 移除复杂缓存
        }
        else
        {
            // 配置 MemoryCache
            services.Configure<MemoryCacheOptions>(options =>
            {
                options.SizeLimit = lybtOptions.Infrastructure.Cache.MemoryCache.SizeLimit;
                options.CompactionPercentage = lybtOptions.Infrastructure.Cache.MemoryCache.CompactionPercentage;
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(lybtOptions.Infrastructure.Cache.MemoryCache.ExpirationScanFrequencySeconds);
            });

            // 使用 MemoryCacheAdapter
            services.AddSingleton<LYBT.Core.Infrastructure.Caching.Interfaces.ICacheService, LYBT.Core.Infrastructure.Caching.Adapters.MemoryCacheAdapter>();
            // services.AddSingleton<LYBT.Infrastructure.Caching.Interfaces.ICacheService, LYBT.Core.Infrastructure.Caching.Adapters.MemoryCacheAdapter>(); // 移除复杂缓存

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
                var jwtConfig = lybtOptions.Authentication.Jwt;
                var issuer = jwtConfig.Issuer;
                var audience = jwtConfig.Audience;
                var clockSkew = 300; // 5分钟时钟偏差（安全默认值）

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // 基本验证设置
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        // 发行者和接收者
                        ValidIssuer = issuer,
                        ValidAudience = audience,

                        // 密钥设置 - 支持多密钥验证
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

                        // 时钟偏差 - 使用配置值
                        ClockSkew = TimeSpan.FromSeconds(clockSkew),

                        // 增强安全设置
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ValidateTokenReplay = false, // 如果需要防重放攻击可设为true

                        // Token类型验证
                        ValidTypes = new[] { "JWT" },

                        // 严格的签名验证
                        TryAllIssuerSigningKeys = true // 启用多密钥验证支持密钥轮换
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

            // 不设置全局回退策略，允许未标注授权属性的端点（如Swagger）匿名访问
            // options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            //     .RequireAuthenticatedUser()
            //     .Build();

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
    /// 注册业务模块（简化版本）。
    /// 使用各模块的静态扩展方法进行注册
    /// </summary>
    private static IServiceCollection RegisterBusinessModules(this IServiceCollection services, IConfiguration configuration)
    {
        // 使用简化的模块注册方法
        // 每个模块负责注册自己的服务、仓储、验证器等

        // 1. 认证模块
        services.AddAuthModule(configuration);

        // 2. 用户模块
        services.AddUsersModule(configuration);

        // 3. 患者模块  
        services.AddPatientsModule(configuration);

        // 4. 中药模块
        services.AddHerbsModule(configuration);

        // 5. 问诊模块
        services.AddConsultationModule(configuration);

        // 6. 处方模块（保持原有注册，等待后续改造）
        services.AddPrescriptionsModule();

        // 7. 配方模块（保持原有注册，等待后续改造）
        services.AddFormulaModule();

        // 8. 病例模块（保持原有注册，等待后续改造）
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

            // 不添加全局安全要求，让每个控制器方法通过[Authorize]特性自己决定是否需要认证
            // 这样Swagger UI本身就不需要认证，只有标记了[Authorize]的API才需要Token
            // c.AddSecurityRequirement(...) -- 已移除全局安全要求

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

        // 如果禁用了速率限制，注册一个空的RateLimiter以避免中间件错误
        if (!rateLimitingConfig.Enabled)
        {
            // 注册一个默认的RateLimiter，但不配置任何限制策略
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                // 不设置GlobalLimiter，相当于禁用
            });
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
