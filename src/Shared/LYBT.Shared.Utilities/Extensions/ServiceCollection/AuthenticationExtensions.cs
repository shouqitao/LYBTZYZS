using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Shared.Utilities.Extensions.ServiceCollection
{
    /// <summary>
    /// JWT认证相关的扩展方法
    /// </summary>
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// 添加JWT Bearer认证
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置对象</param>
        /// <param name="environment">环境名称（可选）</param>
        /// <returns>配置后的服务集合</returns>
        public static IServiceCollection AddJwtBearerAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            string? environment = null)
        {
            // 获取JWT密钥
            var jwtSecret = GetJwtSecret(configuration, environment);

            if (string.IsNullOrEmpty(jwtSecret))
            {
                throw new InvalidOperationException(
                    environment?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true
                        ? "生产环境必须通过JWT_SECRET环境变量或JwtOptions:Secret配置JWT密钥"
                        : "JWT密钥未配置");
            }

            // 获取JWT配置
            var jwtSection = configuration.GetSection("JwtOptions");
            var issuer = jwtSection["Issuer"] ?? "LYBT";
            var audience = jwtSection["Audience"] ?? "LYBT-Client";
            var clockSkew = int.TryParse(jwtSection["ClockSkewSeconds"], out var skew) ? skew : 300;
            var expireMinutes = int.TryParse(jwtSection["ExpireMinutes"], out var expire) ? expire : 480;

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.FromSeconds(clockSkew)
                };
            });

            return services;
        }

        /// <summary>
        /// 获取JWT密钥
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <param name="environment">环境名称</param>
        /// <returns>JWT密钥</returns>
        public static string GetJwtSecret(IConfiguration configuration, string? environment = null)
        {
            // 优先使用环境变量
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                           configuration["JwtOptions:Secret"];

            // 开发环境可使用默认密钥
            if (string.IsNullOrEmpty(jwtSecret))
            {
                var env = environment ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                if (!env.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    jwtSecret = "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction";
                }
            }

            return jwtSecret ?? string.Empty;
        }

        /// <summary>
        /// 获取JWT配置选项
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <returns>JWT配置选项</returns>
        public static JwtOptions GetJwtOptions(IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("JwtOptions");

            return new JwtOptions
            {
                Secret = GetJwtSecret(configuration),
                Issuer = jwtSection["Issuer"] ?? "LYBT",
                Audience = jwtSection["Audience"] ?? "LYBT-Client",
                ExpireMinutes = int.TryParse(jwtSection["ExpireMinutes"], out var expire) ? expire : 480,
                ClockSkewSeconds = int.TryParse(jwtSection["ClockSkewSeconds"], out var skew) ? skew : 300
            };
        }
    }

    /// <summary>
    /// JWT配置选项
    /// </summary>
    public class JwtOptions
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "LYBT";
        public string Audience { get; set; } = "LYBT-Client";
        public int ExpireMinutes { get; set; } = 480;
        public int ClockSkewSeconds { get; set; } = 300;
    }
}