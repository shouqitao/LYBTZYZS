using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Auth {

    /// <summary>
    /// 登录验证模块注册入口
    /// </summary>
    public static class AuthModule {

        /// <summary>
        /// 注册登录验证相关服务 - UltraThink简化架构版
        /// </summary>
        public static IServiceCollection AddAuthModule(this IServiceCollection services) {
            // 注册Repository层
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();

            // 注册核心服务层 - UltraThink双层架构
            services.AddScoped<IAuthQueryService, AuthQueryService>();       // 查询服务层
            services.AddScoped<IAuthBusinessService, AuthBusinessService>(); // 业务服务层
            services.AddScoped<IAuthService, AuthService>();                 // 主服务：纯委托模式
            services.AddScoped<SysAdminHandler>();                           // 管理员特殊处理

            // 注册JWT服务 - 保留核心JWT功能
            services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();

            // 注册配置选项
            services.AddOptions<AuthOptions>();

            return services;
        }
    }
}
