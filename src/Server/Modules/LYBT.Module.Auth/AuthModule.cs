using LYBT.Infrastructure.Options;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Auth
{

    /// <summary>
    /// 登录验证模块注册入口
    /// </summary>
    public static class AuthModule
    {

        /// <summary>
        /// 注册登录验证相关服务
        /// </summary>
        public static IServiceCollection AddAuthModule(this IServiceCollection services)
        {
            // 注册原有仓储
            services.AddScoped<IAuthRepository, AuthRepository>();

            // 注册UltraThink Auth仓储
            services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
            services.AddScoped<ILoginAttemptRepository, LoginAttemptRepository>();
            services.AddScoped<ISecurityLogRepository, SecurityLogRepository>();

            // 注册原有服务
            services.AddScoped<SysAdminHandler>();
            services.AddScoped<IAuthService, AuthService>();

            // 注册UltraThink Auth服务
            services.AddScoped<IAuthSessionService, AuthSessionService>();
            services.AddScoped<ILoginAttemptService, LoginAttemptService>();
            services.AddScoped<ISecurityLogService, SecurityLogService>();

            // 注册JWT相关服务（从Infrastructure迁移而来）
            services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
            services.AddScoped<LYBT.Module.Auth.Interfaces.IAuthorizationService, LYBT.Module.Auth.Services.AuthorizationService>();

            // 注册配置选项
            services.AddOptions<AuthOptions>();

            return services;
        }
    }
}