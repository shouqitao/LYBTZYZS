using LYBT.Infrastructure.Authentication;
using LYBT.Infrastructure.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Auth {

    /// <summary>
    /// 登录验证模块注册入口
    /// </summary>
    public static class AuthModule {

        /// <summary>
        /// 注册登录验证相关服务
        /// </summary>
        public static IServiceCollection AddAuthModule(this IServiceCollection services) {
            // 注册仓储
            services.AddScoped<IAuthRepository, AuthRepository>();

            // 注册服务
            services.AddScoped<SysAdminHandler>();
            services.AddScoped<IAuthService, AuthService>();

            // 注册配置选项
            services.AddOptions<AuthOptions>();

            return services;
        }
    }
}