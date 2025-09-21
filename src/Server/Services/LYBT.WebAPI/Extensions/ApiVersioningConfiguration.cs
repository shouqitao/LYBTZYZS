using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// API版本控制配置 - 统一版本管理策略
/// </summary>
public static class ApiVersioningConfiguration
{
    /// <summary>
    /// 配置API版本控制
    /// </summary>
    public static IServiceCollection ConfigureApiVersioning(this IServiceCollection services)
    {
        // 配置API版本控制
        services.AddApiVersioning(options =>
        {
            // 默认API版本
            options.DefaultApiVersion = new ApiVersion(1, 0);

            // 当客户端没有提供版本时，使用默认版本
            options.AssumeDefaultVersionWhenUnspecified = true;

            // 在响应头中返回支持的API版本
            options.ReportApiVersions = true;

            // API版本读取方式 - 简化为只使用URL段
            // 只保留URL路径版本读取: /api/v1/users
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc() // 为MVC控制器添加版本控制支持
        .AddApiExplorer(options =>
        {
            // 版本格式：'v'VVV，其中VVV是版本号
            options.GroupNameFormat = "'v'VVV";

            // 在URL中替换版本占位符
            options.SubstituteApiVersionInUrl = true;
        });

        // 配置API版本元数据
        services.Configure<ApiExplorerOptions>(options =>
        {
            options.FormatGroupName = (group, version) => $"{group} - v{version}";
        });

        return services;
    }

    /// <summary>
    /// 配置Swagger以支持多版本API文档
    /// </summary>
    public static IServiceCollection ConfigureVersionedSwagger(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            // 获取API版本描述提供者
            var provider = services.BuildServiceProvider()
                .GetRequiredService<IApiVersionDescriptionProvider>();

            // 为每个发现的API版本创建Swagger文档
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    CreateInfoForApiVersion(description, configuration));
            }

            // 添加版本过滤器
            options.OperationFilter<ApiVersionOperationFilter>();

            // 添加已弃用API标记
            options.OperationFilter<DeprecatedOperationFilter>();
        });

        return services;
    }

    /// <summary>
    /// 配置Swagger UI以显示多版本文档
    /// </summary>
    public static IApplicationBuilder UseVersionedSwagger(
        this IApplicationBuilder app,
        IApiVersionDescriptionProvider provider)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // 为每个API版本添加端点
            foreach (var description in provider.ApiVersionDescriptions.Reverse())
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }

            options.RoutePrefix = "swagger";
            options.DocumentTitle = "凌隐宝堂中医诊所API文档";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            options.DefaultModelsExpandDepth(-1);
        });

        return app;
    }

    /// <summary>
    /// 创建API版本信息
    /// </summary>
    private static Microsoft.OpenApi.Models.OpenApiInfo CreateInfoForApiVersion(
        ApiVersionDescription description,
        IConfiguration configuration)
    {
        var info = new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = configuration["Swagger:Title"] ?? "凌隐宝堂中医诊所诊疗系统 API",
            Version = description.ApiVersion.ToString(),
            Description = configuration["Swagger:Description"] ??
                "企业级中医诊所管理系统RESTful API接口文档",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = configuration["Swagger:ContactName"] ?? "技术支持",
                Email = configuration["Swagger:ContactEmail"],
                Url = string.IsNullOrEmpty(configuration["Swagger:ContactUrl"])
                    ? null
                    : new Uri(configuration["Swagger:ContactUrl"])
            },
            License = new Microsoft.OpenApi.Models.OpenApiLicense
            {
                Name = configuration["Swagger:LicenseName"] ?? "Proprietary",
                Url = string.IsNullOrEmpty(configuration["Swagger:LicenseUrl"])
                    ? null
                    : new Uri(configuration["Swagger:LicenseUrl"])
            }
        };

        if (description.IsDeprecated)
        {
            info.Description += "\n\n**此API版本已弃用，请尽快迁移到新版本。**";
        }

        return info;
    }

    /// <summary>
    /// API版本操作过滤器
    /// </summary>
    private class ApiVersionOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // 简化版本参数说明
            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Summary = $"{operation.Summary} (API v1)";
        }
    }

    /// <summary>
    /// 已弃用API操作过滤器
    /// </summary>
    private class DeprecatedOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // 简化弃用检查
            if (context.ApiDescription.GroupName?.Contains("deprecated", StringComparison.OrdinalIgnoreCase) == true)
            {
                operation.Deprecated = true;
                operation.Summary = "⚠️ [已弃用] " + operation.Summary;
                operation.Description = "**警告：此端点已弃用，将在未来版本中移除。**\n\n" +
                                      operation.Description;
            }
        }
    }
}