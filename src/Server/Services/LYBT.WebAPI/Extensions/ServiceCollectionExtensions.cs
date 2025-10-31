using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using LYBT.Infrastructure.Configuration.Extensions;
using LYBT.Module.Auth;
using LYBT.Module.Consultation;
using LYBT.Module.Formula;
using LYBT.Module.Herbs;
using LYBT.Module.MedicalCase;
using LYBT.Module.Patients;
using LYBT.Module.Prescriptions;
using LYBT.Module.Users;
using LYBT.WebAPI.Extensions.ServiceCollection;
using Microsoft.AspNetCore.Mvc;

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

        // 10）环境感知配置校验（生产强校验）- 使用Infrastructure统一实现
        services.AddEnvironmentAwareValidation(environment);

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
    /// 注册控制器与 JSON 选项
    /// </summary>
    private static IServiceCollection RegisterControllerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // =========== UltraThink Phase 2：使用统一配置 ===========
        var lybtOptions = configuration.GetLybtOptions();
        var jsonConfig = lybtOptions.Application.WebApi.Json;

        // 确保 UTF-8 编码可用
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Epic #1731 Phase 3: 配置FluentValidation全局自动验证（使用新API）
        services.AddFluentValidationAutoValidation(config =>
        {
            // 保留DataAnnotations验证（与FluentValidation共存）
            config.DisableDataAnnotationsValidation = false;
        });
        services.AddFluentValidationClientsideAdapters();

        services.AddControllers()
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

        // Epic #1731 Phase 3: 配置自动模型验证行为
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // 启用自动400响应（模型验证失败时）
            options.SuppressModelStateInvalidFilter = false;

            // 自定义400响应格式（使用ProblemDetails）
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "模型验证失败",
                    Detail = "请求数据包含验证错误，请检查输入",
                    Instance = context.HttpContext.Request.Path
                };

                // 添加追踪信息
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

                return new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        return services;
    }
}
