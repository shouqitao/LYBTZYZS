using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.WebAPI.Extensions.ServiceCollection
{
    /// <summary>
    /// JWT认证相关的扩展方法 - 已废弃，请使用UnifiedServiceRegistration
    /// P3配置直读统一：所有配置入口已移至UnifiedServiceRegistration统一管理
    /// </summary>
    [Obsolete("已移至UnifiedServiceRegistration统一管理，避免配置入口分散", false)]
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// 添加JWT Bearer认证 - 已废弃
        /// </summary>
        [Obsolete("请使用UnifiedServiceRegistration.RegisterAllApplicationServices统一注册", false)]
        public static IServiceCollection AddJwtBearerAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            string? environment = null)
        {
            throw new InvalidOperationException(
                "此方法已废弃。请使用UnifiedServiceRegistration.RegisterAllApplicationServices进行统一服务注册。");
        }

        /// <summary>
        /// 获取JWT密钥 - 已废弃
        /// </summary>
        [Obsolete("请直接使用Environment.GetEnvironmentVariable(\"JWT_SECRET\")或IOptions<JwtOptions>", false)]
        public static string GetJwtSecret(IConfiguration configuration, string? environment = null)
        {
            throw new InvalidOperationException(
                "此方法已废弃。请使用Environment.GetEnvironmentVariable(\"JWT_SECRET\")或注入IOptions<JwtOptions>。");
        }

        /// <summary>
        /// 获取JWT配置选项 - 已废弃
        /// </summary>
        [Obsolete("请使用IOptions<LYBT.Infrastructure.Configuration.Options.JwtOptions>注入", false)]
        public static JwtOptions GetJwtOptions(IConfiguration configuration)
        {
            throw new InvalidOperationException(
                "此方法已废弃。请使用IOptions<LYBT.Infrastructure.Configuration.Options.JwtOptions>进行依赖注入。");
        }
    }

    /// <summary>
    /// JWT配置选项 - 已废弃
    /// </summary>
    [Obsolete("请使用LYBT.Infrastructure.Configuration.Options.JwtOptions", false)]
    public class JwtOptions
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "LYBT";
        public string Audience { get; set; } = "LYBT-Client";
        public int ExpireMinutes { get; set; } = 480;
        public int ClockSkewSeconds { get; set; } = 300;
    }
}