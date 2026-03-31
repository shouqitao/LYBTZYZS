using FluentValidation;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Validators.Auth;
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
            services.AddScoped<ITokenManagementService, TokenManagementService>();
            services.AddScoped<IAutoLoginService, AutoLoginService>();
            services.AddScoped<IAuthService, AuthService>();

            // Issue #1870: Token撤销服务
            services.AddScoped<ITokenRevocationService, TokenRevocationService>();

            // Issue #1871: 安全审计服务（需要HttpContextAccessor）
            services.AddHttpContextAccessor();
            services.AddScoped<ISecurityAuditService, SecurityAuditService>();

            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IAutoLoginTokenRepository, AutoLoginTokenRepository>();
            services.AddScoped<ISecurityAuditRepository, SecurityAuditRepository>();

            // Epic #1731: 注册Auth模块Validators
            services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

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
