using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Auth
{
    /// <summary>
    /// 认证模块服务注册（简化版本，遵循适度设计原则）
    /// 仅提供小型中医诊所系统所需的基础认证功能
    /// </summary>
    public static class AuthModule
    {
        /// <summary>
        /// 注册认证模块服务
        /// </summary>
        public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
        {
            // 仅注册必要的核心服务
            services.AddSingleton<IJwtService, JwtService>(); // 优化：JWT服务无状态，使用Singleton
            services.AddScoped<IAuthService, AuthService>();
            return services;
        }
        /// 配置认证模块中间件
        public static IApplicationBuilder UseAuthModule(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }
    }
}
