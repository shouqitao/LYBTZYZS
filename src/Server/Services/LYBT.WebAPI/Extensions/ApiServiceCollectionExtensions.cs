using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.ExceptionHandling.Handlers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
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
    /// 注册 CORS 服务
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        if (corsSection.Exists())
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowConfiguredOrigins", builder =>
                {
                    var origins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                    builder.WithOrigins(origins)
                        .WithMethods(corsSection.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE" })
                        .WithHeaders(corsSection.GetSection("AllowedHeaders").Get<string[]>() ?? Array.Empty<string>())
                        .AllowCredentials();
                });
            });
        }

        return services;
    }

    /// <summary>
    /// 注册 API 文档（Swagger）与统一异常处理
    /// </summary>
    public static IServiceCollection RegisterApiServices(this IServiceCollection services, IConfiguration configuration)
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
        // A-31-C2: 异常处理器统一注册入口（Business 先 System 后，Shared.ExceptionHandling 提供）
        services.AddLybtExceptionHandling();

        // Swagger（含 JWT）- 从配置参数获取配置
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // unify-configuration-system: 使用强类型 SwaggerOptions
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
            // B-16: http/bearer（非 apiKey）——SwaggerUI 自动为输入值加 "Bearer " 前缀，
            // 避免用户直接粘贴裸 Token 时静默 401（apiKey 类型原样发送输入值）
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            // 不添加全局安全要求（匿名端点 login/health/download 会被误标）；改为按操作声明——
            // B-16: [Authorize] 端点由 BearerSecurityRequirementOperationFilter 标注 security，
            // SwaggerUI 才会在 Try it out 时附加 Authorization 头（无 security 声明时仅按钮可用、请求不带 Token）
            c.OperationFilter<BearerSecurityRequirementOperationFilter>();

            // XML 注释 - 使用统一配置控制
            if (swaggerConfig.EnableXmlComments)
            {
                var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                foreach (var xmlFile in xmlFiles)
                    // B-16: includeControllerXmlComments → 控制器类摘要作为 SwaggerUI 分组（tag）说明
                    c.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
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
        // Sprint3-Batch3: 读取配置项，测试环境可通过 Security:RateLimiting:Enabled=false 禁用限流
        var rateLimitingEnabled = configuration.GetValue("Security:RateLimiting:Enabled", true);
        if (!rateLimitingEnabled)
        {
            // 注册无操作 RateLimiter，包含所有策略名以兼容 [EnableRateLimiting("xxx")]
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("Login", _ =>
                    System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("noop"));
                options.AddPolicy("ApiCalls", _ =>
                    System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("noop"));
            });
            return services;
        }

        // MVP阶段：仅启用Login限流防止暴力破解，使用硬编码默认值
        // 默认配置：5次尝试/60秒（合理的防暴力破解策略）
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Sprint3-X6: 返回结构化 ApiResponse + ErrorCode.RateLimitExceeded
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var message = ErrorMessages.Get(ErrorCode.RateLimitExceeded);
                var response = ApiResponse.CreateFail(message, new
                {
                    errorCode = ErrorCode.RateLimitExceeded.ToFormattedString(),
                    retryAfter = context.Lease.TryGetMetadata(
                        System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter)
                        ? retryAfter.TotalSeconds
                        : 60
                });

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };

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

            // Issue 2.3: 全局API调用速率限制：100次请求/分钟
            options.AddPolicy("ApiCalls", httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,      // 每个窗口允许100次请求
                        Window = TimeSpan.FromMinutes(1),  // 1分钟窗口
                        QueueLimit = 0          // 不排队
                    });
            });
        });

        return services;
    }

}

/// <summary>
/// B-16: 按操作声明 Bearer 安全要求——[Authorize] 端点标注 <c>security</c>，[AllowAnonymous] 端点跳过。
/// 缺失该声明时 SwaggerUI 的 Authorize 按钮虽可用，但 Try it out 请求不会附加 Authorization 头。
/// </summary>
internal sealed class BearerSecurityRequirementOperationFilter
    : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(
        Microsoft.OpenApi.Models.OpenApiOperation operation,
        Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var requiresAuth =
            metadata.OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any()
            && !metadata.OfType<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Any();

        if (!requiresAuth)
            return;

        operation.Security.Add(
            new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
    }
}


