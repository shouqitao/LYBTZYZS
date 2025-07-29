using Microsoft.OpenApi.Models;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// Swagger 扩展方法
/// </summary>
public static class SwaggerExtension {

    /// <summary>
    /// 添加 Swagger 服务
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services) {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c => {
            c.SwaggerDoc("v1", new OpenApiInfo {
                Title = "LYBT API",
                Version = "v1"
            });
        });
        return services;
    }

    /// <summary>
    /// 使用 Swagger UI
    /// </summary>
    public static IApplicationBuilder UseSwaggerUIWithDocs(this IApplicationBuilder app) {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LYBT API V1"));
        return app;
    }
}