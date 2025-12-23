using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.ExceptionHandling.Handlers;
using LYBT.WebAPI.Configuration;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// API服务注册扩展
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分
/// 职责：API版本管理、Swagger文档、ProblemDetails、AutoMapper、速率限制
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// 注册 API 文档（Swagger）与统一异常处理
    /// </summary>
    public static IServiceCollection RegisterApiServices(this IServiceCollection services)
    {
        // API版本管理（MVP阶段仅v1.0，简化配置）
        // Issue #1732 Phase 2: 移除3种版本读取器（QueryString/Header/UrlSegment），使用默认行为
        services.AddApiVersioning(options =>
        {
            // 默认API版本：v1.0
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);

            // 当客户端未指定版本时使用默认版本
            options.AssumeDefaultVersionWhenUnspecified = true;

            // 在响应头中报告支持的API版本
            options.ReportApiVersions = true;

            // MVP阶段：仅使用URL路径版本读取器
            // Issue #1887-1892 修复：必须指定UrlSegmentApiVersionReader才能正确解析URL中的v1
            options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
        }).AddMvc().AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // refactor-logging-system: RFC 7807 ProblemDetails + IExceptionHandler处理器链
        services.AddProblemDetailsConfiguration();
        // 异常处理器按优先级注册（先注册的先处理）
        // BusinessExceptionHandler: 处理 AppException 及其子类
        // SystemExceptionHandler: 兜底处理所有未被处理的系统异常
        services.AddExceptionHandler<BusinessExceptionHandler>();
        services.AddExceptionHandler<SystemExceptionHandler>();

        // Swagger（含 JWT）- 从服务提供者获取配置
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // unify-configuration-system: 使用强类型 SwaggerOptions
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var swaggerConfig = new SwaggerOptions();
            configuration.GetSection(SwaggerOptions.SectionName).Bind(swaggerConfig);

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
    /// 配置速率限制（仅Login端点防暴力攻击）
    /// Issue #1732 Phase 2: 简化为单层Login限流（MVP合规）
    /// Issue #1761 Phase 2.1: 使用硬编码默认值，移除配置依赖（MVP简化）
    /// </summary>
    public static IServiceCollection ConfigureRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // MVP阶段：仅启用Login限流防止暴力破解，使用硬编码默认值
        // 默认配置：5次尝试/60秒（合理的防暴力破解策略）
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 登录端点速率限制：基于IP的固定窗口限流器
            options.AddPolicy("Login", httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,        // 每个窗口允许5次尝试
                        Window = TimeSpan.FromSeconds(60),  // 60秒窗口
                        QueueLimit = 0          // 不排队
                    });
            });
        });

        return services;
    }
}
