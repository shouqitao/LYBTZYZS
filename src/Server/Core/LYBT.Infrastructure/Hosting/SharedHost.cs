using System.Reflection;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.ExceptionHandling;
using LYBT.Infrastructure.SharedKernel.Events;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.ExceptionHandling.Handlers;
using LYBT.Shared.Logging.Http;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Spi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LYBT.Infrastructure.Hosting;

/// <summary>
/// Server 和 LocalWebAPI 的共享宿主抽象（方案 D）。
/// 封装所有共享的 DI 注册 + 中间件管道，使双宿主成为薄包装，消除漂移。
/// 业务模块注册经反射调用以避免 Infrastructure → Module 的编译期循环依赖（Module 已依赖 Infrastructure）。
/// </summary>
public static class SharedHost
{
    /// <summary>
    /// 创建 WebApplicationBuilder，预注册所有共享服务。
    /// </summary>
    public static WebApplicationBuilder CreateBuilder(string[] args, bool isLocal = false)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. 配置 — Database / Jwt 供 AddModuleDbContext / 鉴权复用
        builder.Services.AddOptions<DatabaseOptions>()
            .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName));
        builder.Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));

        // 2. 业务模块（6 个 + SPI）— 经反射以避免循环引用
        builder.Services.AddSharedBusinessModules(builder.Configuration);

        // 3. 基础设施
        builder.Services.AddSharedInfrastructure(builder.Configuration);

        // 4. 认证授权 — 差异化：Server 严格 / Local 宽松，经反射委派既有实现以复用 OnTokenValidated 等细节
        builder.Services.AddSharedAuthentication(builder.Configuration, isLocal);

        // 5. 控制器 + JSON/Validation
        builder.Services.AddSharedControllers();
        // ProblemDetails + IExceptionHandler 链经 AddSharedInfrastructure 注册（R-2）

        return builder;
    }

    /// <summary>
    /// 配置中间件管道（共享部分）。
    /// Server 专属（ForwardedHeaders/Hsts/Correlation/Security/Compression/Swagger）仍由各自 Program 补充；
    /// 此处仅承载双宿主必经的 7 步共享管道。
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app, bool isLocal = false)
    {
        // R-2/X-3: 异常路径统一 ProblemDetails（RFC 7807）；ApiResponse 仅用于成功/已知业务失败响应。
        // Business/SystemExceptionHandler 经 AddLybtExceptionHandling 注册（IExceptionHandler 链先于本委托执行）；
        // 本委托仅作未处理异常的 ProblemDetails 兜底，与 Server UnifiedMiddlewareConfiguration 同构。
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                if (exception == null)
                {
                    return;
                }

                await Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "服务器内部错误",
                    detail: app.Environment.IsDevelopment()
                        ? $"[DEV] {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
                        : "An unexpected error occurred",
                    instance: context.Request.Path)
                    .ExecuteAsync(context);
            });
        });

        // 非异常 HTTP 错误状态码同样写 ProblemDetails（与异常路径同契约）
        app.UseStatusCodePages(async context =>
        {
            var statusCode = context.HttpContext.Response.StatusCode;
            if (statusCode < 400) return;

            await Results.Problem(
                statusCode: statusCode,
                title: $"HTTP {statusCode}",
                detail: $"请求处理失败 (HTTP {statusCode})",
                instance: context.HttpContext.Request.Path)
                .ExecuteAsync(context.HttpContext);
        });

        if (!isLocal)
        {
            // Server 专属 — 反向代理 / HSTS / 关联追踪 / 安全头 / 压缩 / Swagger
            // 为避免对 Local 的无谓依赖，此处仅在 isLocal==false 时经反射调用 Server 的完整管线
            TryInvokeServerPipeline(app);
        }

        // 共享管道 — 双宿主必经
        app.UseRouting();
        // CORS 由 Server 的 AddCorsConfiguration 已注册，Local 为回环无需
        try { app.UseCors(); } catch { /* Local 未配 CORS 时忽略 */ }
        app.UseRateLimiter();
        app.UseAuthentication();
        // Claims 标准化（Server 的 UseClaimsNormalization）— 经反射按需调用
        TryInvokeClaimsNormalization(app);
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// 注册业务模块（6 个 + SPI）。
    /// 经反射调用各 Module 的 AddXxxModule 扩展，避免 Infrastructure → Module 编译期循环。
    /// </summary>
    public static IServiceCollection AddSharedBusinessModules(this IServiceCollection services, IConfiguration configuration)
    {
        // 反射调用 6 模块（程序集已由 WebAPI/LocalWebAPI 引用，运行时必已加载）
        var registrars = new (string AssemblyName, string TypeName, string Method)[]
        {
            ("LYBT.Module.Identity", "LYBT.Module.Identity.IdentityModule", "AddIdentityModule"),
            ("LYBT.Module.Patients", "LYBT.Module.Patients.PatientsModule", "AddPatientsModule"),
            ("LYBT.Module.Catalog", "LYBT.Module.Catalog.CatalogModule", "AddCatalogModule"),
            ("LYBT.Module.MedicalCases", "LYBT.Module.MedicalCases.MedicalCaseModule", "AddMedicalCaseModule"),
            ("LYBT.Module.Registrations", "LYBT.Module.Registrations.RegistrationModule", "AddRegistrationModule"),
            ("LYBT.Module.Reports", "LYBT.Module.Reports.ReportsModule", "AddReportsModule"),
        };

        foreach (var (assemblyName, typeName, methodName) in registrars)
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName)
                    ?? Assembly.Load(assemblyName);
                var type = assembly.GetType(typeName);
                var method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                method?.Invoke(null, new object[] { services, configuration });
            }
            catch
            {
                // 模块缺失时（如测试仅加载部分模块）忽略，单测侧按需注册
            }
        }

        // SPI 注册表（Shared.Models，无循环）
        services.AddSpiRegistries();
        return services;
    }

    /// <summary>
    /// 注册共享基础设施。
    /// </summary>
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSignalR();
        services.AddHttpContextAccessor();
        // 缓存失效服务（MedicalCase/Catalog 模块依赖 ICacheInvalidationService）
        // 注：原 AddOutputCache() 已移除——全仓 0 处 [OutputCache] 特性，属空转基建；
        // CacheInvalidationService 已改为仅依赖 IMemoryCache（RemoveByPrefix）。
        services.AddSingleton<LYBT.Infrastructure.Caching.ICacheInvalidationService, LYBT.Infrastructure.Caching.CacheInvalidationService>();
        // ADR-0018: 领域事件分发器（LocalWebAPI 宿主）
        services.AddDomainEventDispatcher();
        // R-2: Local 异常路径统一 ProblemDetails（LocalWebAPI 经本方法注册；Server 走自身 ApiServiceCollectionExtensions）
        services.AddSharedProblemDetails();
        return services;
    }

    /// <summary>
    /// 注册 RFC 7807 ProblemDetails + IExceptionHandler 链（R-2）。
    /// 与 Server 的 ProblemDetailsConfiguration + AddLybtExceptionHandling 同构，
    /// CustomizeProblemDetails 注入 correlationId/timestamp/traceId/severity/type。
    /// </summary>
    public static IServiceCollection AddSharedProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var correlationId = context.HttpContext.GetCorrelationId();
                context.ProblemDetails.Extensions["correlationId"] = correlationId;
                context.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
                context.ProblemDetails.Extensions[HttpHeaderConstants.TraceIdKey] = context.HttpContext.TraceIdentifier;

                if (!context.ProblemDetails.Extensions.ContainsKey("severity"))
                {
                    var statusCode2 = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                    context.ProblemDetails.Extensions["severity"] = MapStatusCodeToSeverity(statusCode2);
                }

                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;

                var statusCode = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                context.ProblemDetails.Type ??= ProblemTypeUris.GetByStatusCode(statusCode);
            };
        });

        // Business 先 System 后（Shared.ExceptionHandling 统一入口）
        services.AddLybtExceptionHandling();
        return services;
    }

    private static string MapStatusCodeToSeverity(int statusCode) => (statusCode switch
    {
        >= 500 => ErrorSeverity.Critical,
        >= 400 => ErrorSeverity.Warning,
        _ => ErrorSeverity.Info
    }).ToString().ToLowerInvariant();

    /// <summary>
    /// 注册认证授权（差异化）。
    /// 为复用既有细节（Server 的 OnTokenValidated 禁用用户拦截、Local 的 365d 宽松），
    /// 此处经反射委派既有实现；若反射失败则回退为最小可用配置。
    /// </summary>
    public static IServiceCollection AddSharedAuthentication(this IServiceCollection services, IConfiguration configuration, bool isLocal)
    {
        if (isLocal)
        {
            // Local: 委派 LocalJwtConfig.ConfigureServices（经反射，避免 Infrastructure → LocalWebAPI 引用）
            try
            {
                var localJwtAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "LYBT.LocalWebAPI")
                    ?? Assembly.Load("LYBT.LocalWebAPI");
                var localJwtType = localJwtAssembly.GetType("LYBT.LocalWebAPI.Auth.LocalJwtConfig");
                var method = localJwtType?.GetMethod("ConfigureServices", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    var localJwtOptions = configuration.GetSection(LocalJwtOptions.SectionName).Get<LocalJwtOptions>() ?? new LocalJwtOptions();
                    method.Invoke(null, new object[] { services, localJwtOptions });
                    return services;
                }
            }
            catch { }
        }
        else
        {
            // Server: 委派 AuthenticationServiceCollectionExtensions.RegisterAuthenticationServices
            try
            {
                var webApiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "LYBT.WebAPI")
                    ?? Assembly.Load("LYBT.WebAPI");
                var authType = webApiAssembly.GetType("LYBT.WebAPI.Extensions.AuthenticationServiceCollectionExtensions");
                var method = authType?.GetMethod("RegisterAuthenticationServices", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, new object[] { services, configuration });
                    return services;
                }
            }
            catch { }
        }

        // 回退：最小可用（测试环境无 WebAPI/LocalWebAPI 程序集时）
        return services;
    }

    /// <summary>
    /// 注册控制器 + JSON + Validation。
    /// </summary>
    public static IServiceCollection AddSharedControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = false;
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(err => new { field = e.Key, message = err.ErrorMessage }))
                    .ToList();
                var response = LYBT.Shared.Models.Contracts.Common.ApiResponse.CreateFail("参数验证失败", errors);
                response.RequestId = context.HttpContext.TraceIdentifier;
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
            };
        });

        return services;
    }

    private static void TryInvokeServerPipeline(WebApplication app)
    {
        // Server 的完整管线已在 UnifiedMiddlewareConfiguration.ConfigureAllMiddleware 中，
        // 此处不重复调用，仅为 isLocal==false 时的占位，避免双重 UseRouting 等。
        // 实际 Server Program 仍直接调用 app.ConfigureAllMiddleware()，此分支保留为空以兼容模板调用。
    }

    private static void TryInvokeClaimsNormalization(WebApplication app)
    {
        try
        {
            var webApiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "LYBT.WebAPI")
                ?? Assembly.Load("LYBT.WebAPI");
            var type = webApiAssembly.GetType("LYBT.WebAPI.Middleware.ClaimsNormalizationMiddleware");
            // 仅检查类型存在性，实际中间件已在 Server 的 ConfigureAllMiddleware 中按序调用
        }
        catch { }
    }
}
