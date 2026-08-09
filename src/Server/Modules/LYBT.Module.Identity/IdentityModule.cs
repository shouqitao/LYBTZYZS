using FluentValidation;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Validation;
using LYBT.Module.Identity.Application.Commands;
using LYBT.Module.Identity.Application.Validators;
using LYBT.Module.Identity.Infrastructure;
using LYBT.Module.Identity.Interfaces;
using LYBT.Module.Identity.Services;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Models.Validators.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Identity
{
    /// <summary>
    /// 认证用户模块服务注册（A-31-C3a 合并 AuthModule + UsersModule）。
    /// 以 ASP.NET Identity 为核心 + AuthSession/SecurityAuditLog 业务增强层。
    /// </summary>
    public static class IdentityModule
    {
        /// <summary>
        /// 注册认证用户模块服务
        /// </summary>
        public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册 DbContext（模块级，同库逻辑隔离）
            services.AddModuleDbContext<IdentityDbContext>(configuration);

            // 注册仓储
            services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
            services.AddScoped<ISecurityAuditRepository, SecurityAuditRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // 注册核心服务
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<ISecurityAuditService, SecurityAuditService>();
            services.AddScoped<IUserService, UserService>();

            // 登录流程选项（Local 登录差异控制；缺省 Remote 全量）
            services.AddOptions<LoginOptions>()
                .Bind(configuration.GetSection(LoginOptions.SectionName));

            // 注册 MediatR（Application层，17 Command/Query）
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Mapperly映射器 - 静态类直接调用（合并 AuthUserMapper/UserMapper/UserCrossModuleMapper）

            // 注册 Validators（LoginRequest 验证 SSOT 为 Shared 版；Users 验证器迁入本模块）
            services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

            return services;
        }
    }
}
