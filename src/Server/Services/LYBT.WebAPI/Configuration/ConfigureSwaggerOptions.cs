using Asp.Versioning.ApiExplorer;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.WebAPI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LYBT.WebAPI.Configuration;

/// <summary>
/// Swagger 文档按 API 版本生成（F-07 / ADR-0015: URL Path Versioning）
/// <para>
/// 每个被发现的版本（<see cref="IApiVersionDescriptionProvider"/>，组名格式 <c>'v'VVV</c>）产出一个 Swagger 文档：
/// v1 → <c>/swagger/v1/swagger.json</c>。新增 v2 时（对受影响控制器加 <c>[ApiVersion("2")]</c> + 路由）
/// 文档与 SwaggerUI 端点自动跟随，无需改动 Swagger 注册代码。
/// </para>
/// <para>
/// 只有 v1 时输出与历史硬编码单文档一致（同样的文档名 v1 / 路径 /swagger/v1/swagger.json）。
/// </para>
/// </summary>
public sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly IConfiguration _configuration;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IConfiguration configuration)
    {
        _provider = provider;
        _configuration = configuration;
    }

    public void Configure(SwaggerGenOptions options)
    {
        // unify-configuration-system: 使用强类型 SwaggerOptions
        var swaggerConfig = new SwaggerOptions();
        _configuration.GetSection(SwaggerOptions.SectionName).Bind(swaggerConfig);

        foreach (var description in _provider.ApiVersionDescriptions)
        {
            var apiDescription = swaggerConfig.Description;
            if (description.IsDeprecated)
                apiDescription += " [Deprecated]";

            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = swaggerConfig.Title,
                // 文档版本号与文档名/URL 段一致（'v'VVV → v1）
                Version = description.GroupName,
                Description = apiDescription,
                Contact = new OpenApiContact
                {
                    Name = swaggerConfig.ContactName,
                    Email = swaggerConfig.ContactEmail,
                    Url = !string.IsNullOrEmpty(swaggerConfig.ContactUrl) ? new Uri(swaggerConfig.ContactUrl) : null
                },
                License = new OpenApiLicense
                {
                    Name = swaggerConfig.LicenseName,
                    Url = !string.IsNullOrEmpty(swaggerConfig.LicenseUrl) ? new Uri(swaggerConfig.LicenseUrl) : null
                }
            });
        }

        // JWT Bearer security definition
        // B-16: http/bearer（非 apiKey）——SwaggerUI 自动为输入值加 "Bearer " 前缀，
        // 避免用户直接粘贴裸 Token 时静默 401（apiKey 类型原样发送输入值）
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        // 不添加全局安全要求（匿名端点 login/health/download 会被误标）；改为按操作声明——
        // B-16: [Authorize] 端点由 BearerSecurityRequirementOperationFilter 标注 security，
        // SwaggerUI 才会在 Try it out 时附加 Authorization 头（无 security 声明时仅按钮可用、请求不带 Token）
        options.OperationFilter<BearerSecurityRequirementOperationFilter>();

        // XML 注释 - 使用统一配置控制
        if (swaggerConfig.EnableXmlComments)
        {
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
            foreach (var xmlFile in xmlFiles)
                // B-16: includeControllerXmlComments → 控制器类摘要作为 SwaggerUI 分组（tag）说明
                options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
        }

        // 避免 Schema ID 冲突
        options.CustomSchemaIds(GetSchemaId);
    }

    /// <summary>Schema ID 生成（泛型展开 + 去重命名空间/嵌套分隔符）</summary>
    private static string GetSchemaId(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericTypeName = genericDef.FullName?.Split('`')[0]?.Replace(".", string.Empty) ?? genericDef.Name.Split('`')[0];

            var genericArgs = type.GetGenericArguments()
                .Select(GetTypeSignature)
                .ToArray();

            return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
        }

        return type.FullName?.Replace(".", string.Empty).Replace("+", string.Empty) ?? type.Name;
    }

    /// <summary>生成 Schema ID 的辅助方法（泛型参数签名）</summary>
    private static string GetTypeSignature(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericTypeName = genericDef.Name.Split('`')[0];
            var genericArgs = type.GetGenericArguments()
                .Select(GetTypeSignature)
                .ToArray();
            return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
        }

        return type.Name.Replace("[]", "Array");
    }
}
