using LYBT.Infrastructure.Options;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;
using LYBT.Module.Auth.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Auth
{

    /// <summary>
    /// 登录验证模块注册入口
    /// </summary>
    public static class AuthModule
    {

        /// <summary>
        /// 注册登录验证相关服务 - UltraThink三层架构版
        /// </summary>
        public static IServiceCollection AddAuthModule(this IServiceCollection services)
        {
            // 注册原有仓储
            services.AddScoped<IAuthRepository, AuthRepository>();

            // 注册UltraThink Auth仓储（简化版）
            services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
            // UltraThink v2.0简化：移除ILoginAttemptRepository、ISecurityLogRepository

            // 注册原有服务
            services.AddScoped<SysAdminHandler>();

            // 注册UltraThink三层架构服务
            services.AddScoped<AuthServiceCore>();        // Core层：基础CRUD和认证
            services.AddScoped<AuthQueryService>();       // Query层：查询和Token验证
            services.AddScoped<AuthBusinessService>();    // Business层：复杂业务逻辑
            services.AddScoped<IAuthService, AuthService>(); // 主服务：纯委托模式

            // 注册UltraThink Auth服务（简化版）
            services.AddScoped<IAuthSessionService, AuthSessionService>();
            // UltraThink v2.0简化：移除ILoginAttemptService、ISecurityLogService

            // 注册JWT相关服务（从Infrastructure迁移而来）
            services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
            services.AddScoped<LYBT.Module.Auth.Interfaces.IAuthorizationService, LYBT.Module.Auth.Services.AuthorizationService>();

            // UltraThink三层架构：移除Helper模式，Helper功能分解到三层服务中
            // services.AddScoped<AuthValidationHelper>();  // 功能迁移到AuthServiceCore
            // services.AddScoped<AuthSessionHelper>();     // 功能迁移到AuthQueryService
            // services.AddScoped<AuthLoggingHelper>();     // 功能迁移到AuthBusinessService

            // 注册配置选项
            services.AddOptions<AuthOptions>();

            return services;
        }
    }
}