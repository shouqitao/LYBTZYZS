using Microsoft.Extensions.Configuration;

namespace LYBT.WebAPI.Configuration;

/// <summary>
/// Swagger 可用性判定 SSOT（SWAGGER-TOGGLE）：非生产环境默认启用；生产环境默认关闭，
/// 仅 <c>Swagger:Enabled=true</c>（配置文件或环境变量 <c>Swagger__Enabled</c>）显式开启。
/// B-16: 中间件装配 / 匿名兜底端点 / 安全头 CSP 豁免 / 下载主页入口 四处共用同一判定，避免复制漂移。
/// </summary>
internal static class SwaggerAvailability
{
    /// <summary>Swagger 在当前环境与配置下是否可用</summary>
    public static bool IsEnabled(IConfiguration configuration, IHostEnvironment environment)
        => configuration.GetValue<bool>("Swagger:Enabled") || !environment.IsProduction();
}
