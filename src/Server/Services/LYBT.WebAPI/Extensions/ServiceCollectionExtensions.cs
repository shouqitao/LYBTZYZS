using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using LYBT.Infrastructure.Serialization;
using LYBT.WebAPI.Serialization;
using LYBT.Module.Auth;
using LYBT.Module.Formulas;
using LYBT.Module.Herbs;
using LYBT.Module.MedicalCases;
using LYBT.Module.Patients;
using LYBT.Module.Registration;
using LYBT.Module.Sync;
using LYBT.Module.Users;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.WebAPI.Filters;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using LybtJsonOptions = LYBT.Shared.Configuration.Options.Server.JsonOptions;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 服务注册主入口扩展
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分并重组
/// 职责：主入口协调、业务模块注册、控制器配置
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册应用服务（统一入口）
    /// </summary>
    public static IServiceCollection RegisterAllApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 1）基础设施（数据库、缓存、健康检查）
        services.RegisterInfrastructureServices(configuration);

        // 2）认证与安全（JWT、授权策略）
        services.RegisterAuthenticationServices(configuration);

        // 3）业务模块
        services.RegisterBusinessModules(configuration);

        // 4）API 文档（Swagger、API版本、ProblemDetails、AutoMapper）
        services.RegisterApiServices();

        // 5）控制器与 JSON（FluentValidation、JSON序列化）
        services.RegisterControllerServices(configuration);

        // 6）速率限制（Login端点防暴力攻击）
        services.ConfigureRateLimiting(configuration, environment);

        // 7）性能优化（服务注册）
        services.ConfigurePerformanceOptimizations(configuration);

        // 8）API 版本管理（已移至RegisterApiServices）
        // services.ConfigureApiVersioning();

        // 9）安全服务（数据保护、密钥管理、密钥旋转）
        services.AddSecurityServices(configuration, environment);

        // 10）环境感知配置校验 - 已移除，统一使用 LYBT.Shared.Configuration 配置验证

        return services;
    }

    /// <summary>
    /// 注册业务模块（简化版本）
    /// 使用各模块的静态扩展方法进行注册
    /// </summary>
    private static IServiceCollection RegisterBusinessModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 使用简化的模块注册方法
        // 每个模块负责注册自己的服务、仓储、验证器等

        // 1. 认证模块
        services.AddAuthModule(configuration);

        // 2. 挂号模块 - Sprint 2
        // 必须在 Users 和 MedicalCase 之前注册，因为 UserService 和 MedicalCaseCommandService 依赖 IRegistrationRepository
        services.AddRegistrationModule();

        // 3. 用户模块
        services.AddUsersModule(configuration);

        // 4. 患者模块
        services.AddPatientsModule(configuration);

        // 5. 中药模块
        services.AddHerbsModule(configuration);

        // 6. 配方模块
        services.AddFormulaModule();

        // OpenSpec: refactor-server-srp-patterns - Consultation/Prescriptions模块已删除
        // 诊断和处方功能已整合到MedicalCase聚合根

        // 7. 病例模块
        services.AddMedicalCaseModule();

        // 8. 同步模块 - OpenSpec: implement-data-sync
        services.AddSyncModule(configuration);

        return services;
    }

    /// <summary>
    /// 注册控制器与 JSON 选项
    /// </summary>
    private static IServiceCollection RegisterControllerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // unify-configuration-system: 使用强类型 JsonOptions
        var jsonConfig = new LybtJsonOptions();
        configuration.GetSection(LybtJsonOptions.SectionName).Bind(jsonConfig);

        // 确保 UTF-8 编码可用
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Epic #1731 Phase 3: 配置FluentValidation全局自动验证（使用新API）
        services.AddFluentValidationAutoValidation(config =>
        {
            // 保留DataAnnotations验证（与FluentValidation共存）
            config.DisableDataAnnotationsValidation = false;
        });
        services.AddFluentValidationClientsideAdapters();

        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
            options.ValueLengthLimit = 1024 * 1024;
            options.MultipartHeadersLengthLimit = 32768;
            options.MemoryBufferThreshold = 1024 * 1024;
        });

        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
        });

        services.AddControllers(options =>
            {
                // OpenSpec: enhance-dataflow-logging - 全局API日志过滤器
                options.Filters.Add<ApiLoggingFilter>();
            })
            .AddJsonOptions(options =>
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
                // Issue #2254: 敏感数据脱敏转换器
                options.JsonSerializerOptions.Converters.Add(new SensitiveDataJsonConverterFactory());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = false;
                options.JsonSerializerOptions.IgnoreReadOnlyProperties = jsonConfig.IgnoreReadOnlyProperties;
                options.JsonSerializerOptions.AllowTrailingCommas = jsonConfig.AllowTrailingCommas;

                // JSON 编码：使用统一配置的设置
                options.JsonSerializerOptions.Encoder = jsonConfig.UnsafeRelaxedEscaping
                    ? JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    : JavaScriptEncoder.Default;

                // Issue 4.2: JSON Source Generation - 使用编译时生成的序列化器
                options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, LybtJsonContext.Default);
            });

        // Epic #1731 Phase 3: 配置自动模型验证行为
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // 启用自动400响应（模型验证失败时）
            options.SuppressModelStateInvalidFilter = false;

            // 自定义400响应格式（使用ApiResponse统一格式）
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(err => $"{e.Key}: {err.ErrorMessage}"))
                    .ToList();

                var response = LYBT.Shared.Models.Contracts.Common.ApiResponse.CreateFail(
                    "参数验证失败",
                    errors
                );
                response.RequestId = context.HttpContext.TraceIdentifier;
                response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                return new BadRequestObjectResult(response);
            };
        });

        return services;
    }

    /// <summary>
    /// 配置性能优化服务
    /// Issue #1732 Phase 3: 仅保留响应压缩配置
    /// 响应缓存、输出缓存、健康检查已在DatabaseServiceCollectionExtensions中配置
    /// </summary>
    private static IServiceCollection ConfigurePerformanceOptimizations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 响应压缩（Brotli + Gzip）
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/xml",
                "text/json",
                "text/xml"
            });
        });

        // 配置Brotli压缩级别
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        // 配置Gzip压缩级别
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        return services;
    }

    /// <summary>
    /// 添加安全相关服务（ASP.NET Core DataProtection）
    /// Issue #1743: 仅保留ASP.NET Core DataProtection配置
    /// </summary>
    private static IServiceCollection AddSecurityServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 配置数据保护（ASP.NET Core密钥管理）
        services.AddDataProtection()
            .SetApplicationName("LYBT")
            .PersistKeysToFileSystem(new DirectoryInfo(
                Path.Combine(environment.ContentRootPath, "DataProtection-Keys")));

        if (environment.IsProduction())
        {
            // 生产环境：使用证书保护密钥（可选，未来配置）
            // services.AddDataProtection().ProtectKeysWithCertificate(certificate);
        }

        return services;
    }
}
