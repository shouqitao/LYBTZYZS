namespace LYBT.WebAPI.Extensions;

/// <summary>
/// CORS 扩展方法
/// </summary>
public static class CorsExtension {

    /// <summary>
    /// 注册跨域策略
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services) {
        services.AddCors(options => {
            options.AddPolicy("AllowAll", builder =>
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });
        return services;
    }
}
