using Microsoft.Extensions.DependencyInjection;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;

namespace LYBT.Module.Auth {
    /// <summary>
    /// 登录验证模块注册入口
    /// </summary>
    public static class AuthModule {
        /// <summary>
        /// 注册登录验证相关服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IAuthService, AuthService>();
        }
    }
}
