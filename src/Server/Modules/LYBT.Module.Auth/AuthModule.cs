using FluentValidation;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Validation;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Validators.Auth;
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
            // 注册 DbContext（模块级）
            services.AddModuleDbContext<Infrastructure.AuthDbContext>(configuration);

            // 注册仓储
            services.AddScoped<Interfaces.IAuthSessionRepository, Infrastructure.AuthSessionRepository>();
            services.AddScoped<Interfaces.ISecurityAuditRepository, Infrastructure.SecurityAuditRepository>();

            // 注册核心服务
            services.AddScoped<Interfaces.IJwtService, Services.JwtService>();
            services.AddScoped<Interfaces.ISecurityAuditService, Services.SecurityAuditService>();

            // 注册跨模块服务（供 Users 等模块触发令牌撤销与安全审计）
            services.AddScoped<IAuthCrossModuleService, Services.AuthCrossModuleService>();

            // 注册 MediatR（Application层）
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Application.Commands.LoginCommand).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Mapperly映射器 - 无状态单例
            services.AddSingleton<Application.Mappers.AuthUserMapper>();

            // Epic #1731: 注册Auth模块Validators（LoginRequest 验证 SSOT 为 Shared 版，A-28 P1-5 收敛）
            services.AddValidatorsFromAssemblyContaining<LYBT.Shared.Models.Validators.Auth.LoginRequestValidator>();

            return services;
        }

    }
}


