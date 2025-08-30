using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace LYBT.WebAPI.Extensions
{
    /// <summary>
    /// Swagger扩展配置
    /// </summary>
    public static class SwaggerExtensions
    {
        /// <summary>
        /// 添加版本化的Swagger文档
        /// </summary>
        public static IServiceCollection AddVersionedSwagger(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            
            services.AddSwaggerGen(options =>
            {
                // 添加安全定义
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // 包含XML注释
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                // 自定义操作过滤器
                options.OperationFilter<RemoveVersionFromParameter>();
                options.DocumentFilter<ReplaceVersionWithExactValueInPath>();

                // 解决Schema ID冲突
                options.CustomSchemaIds(type => GetSchemaId(type));
            });

            return services;
        }

        /// <summary>
        /// 使用版本化的Swagger
        /// </summary>
        public static IApplicationBuilder UseVersionedSwagger(this IApplicationBuilder app, IApiVersionDescriptionProvider provider)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                // 为每个发现的API版本构建一个swagger端点
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
                }

                // 设置默认模型展开深度
                options.DefaultModelExpandDepth(2);
                options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                options.EnableDeepLinking();
                options.DisplayOperationId();
            });

            return app;
        }

        private static string GetSchemaId(Type type)
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var genericTypeName = genericDef.FullName?.Split('`')[0]?.Replace(".", "") ?? genericDef.Name.Split('`')[0];

                var genericArgs = type.GetGenericArguments()
                    .Select(arg => GetSchemaId(arg))
                    .ToArray();

                return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
            }

            return type.FullName?.Replace(".", "").Replace("+", "") ?? type.Name;
        }
    }

    /// <summary>
    /// 配置Swagger选项
    /// </summary>
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        public void Configure(SwaggerGenOptions options)
        {
            // 为每个API版本创建文档
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }
        }

        private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo
            {
                Title = "凌隐宝堂中医诊所诊疗系统 API",
                Version = description.ApiVersion.ToString(),
                Description = "凌隐宝堂中医诊所诊疗系统API文档",
                Contact = new OpenApiContact
                {
                    Name = "技术支持",
                    Email = "support@lybt.com"
                },
                License = new OpenApiLicense
                {
                    Name = "专有软件许可",
                    Url = new Uri("https://lybt.com/license")
                }
            };

            if (description.IsDeprecated)
            {
                info.Description += " **此API版本已被弃用！**";
            }

            return info;
        }
    }

    /// <summary>
    /// 从参数中移除版本
    /// </summary>
    public class RemoveVersionFromParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var versionParameter = operation.Parameters.SingleOrDefault(p => p.Name == "version");
            operation.Parameters.Remove(versionParameter);
        }
    }

    /// <summary>
    /// 替换路径中的版本占位符
    /// </summary>
    public class ReplaceVersionWithExactValueInPath : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = new OpenApiPaths();
            foreach (var path in swaggerDoc.Paths)
            {
                paths.Add(path.Key.Replace("v{version}", swaggerDoc.Info.Version), path.Value);
            }
            swaggerDoc.Paths = paths;
        }
    }
}