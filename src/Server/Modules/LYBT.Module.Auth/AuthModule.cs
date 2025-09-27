using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Auth
{
    /// <summary>
    /// 认证模块服务注册（简化版本）
    /// </summary>
    public static class AuthModule
    {
        /// <summary>
        /// 注册认证模块服务
        /// </summary>
        public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册JWT服务
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IEnhancedJwtService, EnhancedJwtService>();
            
            // 注册认证服务
            services.AddScoped<IAuthService, AuthService>();
            
            // 注册密钥服务
            services.AddScoped<ISecurityKeyService, SecurityKeyService>();
            services.AddScoped<ISecurityKeyRepository, SecurityKeyRepository>();
            
            // 注册RefreshToken仓储
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            
            // 注册验证器
            services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
            services.AddScoped<IValidator<RefreshTokenDto>, RefreshTokenDtoValidator>();
            
            return services;
        }
        
        /// <summary>
        /// 配置认证模块中间件
        /// </summary>
        public static IApplicationBuilder UseAuthModule(this IApplicationBuilder app)
        {
            // 启用认证中间件
            app.UseAuthentication();
            app.UseAuthorization();
            
            return app;
        }
        
        /// <summary>
        /// 验证模块健康状态
        /// </summary>
        public static IHealthChecksBuilder AddAuthModuleHealthCheck(this IHealthChecksBuilder builder)
        {
            return builder.AddCheck<AuthModuleHealthCheck>("auth_module");
        }
    }
}
    public static class AuthModule
    {

        /// <summary>
        /// 注册登录验证相关服务 - UltraThink简化架构版
        /// </summary>
        public static IServiceCollection AddAuthModule(this IServiceCollection services)
        {
            // 注册Repository层
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // 注册核心服务层 - 简化架构
            services.AddScoped<IAuthService, AuthService>();                 // 主服务：纯委托模式
            services.AddScoped<SysAdminHandler>();                           // 管理员特殊处理

            // 注册JWT服务 - 保留核心JWT功能
            services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
            
            // 注册增强的JWT服务（支持RefreshToken）
            services.AddScoped<IEnhancedJwtService, EnhancedJwtService>();
            services.AddScoped<ISecurityKeyService, SecurityKeyService>();
            
            // JWT黑名单服务由Infrastructure层提供

            // 注册配置选项
            services.AddOptions<AuthOptions>();
            services.AddOptions<SysAdminOptions>();

            return services;
        }
    }
}
