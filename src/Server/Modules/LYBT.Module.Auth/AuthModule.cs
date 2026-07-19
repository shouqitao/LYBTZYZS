using FluentValidation;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Validators.Auth;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            services.AddDbContext<Infrastructure.AuthDbContext>((sp, options) =>
            {
                var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
                options.UseSqlServer(dbOptions.ConnectionString);
            });

            // 注册仓储
            services.AddScoped<Interfaces.IAuthSessionRepository, Infrastructure.AuthSessionRepository>();
            services.AddScoped<Interfaces.ISecurityAuditRepository, Infrastructure.SecurityAuditRepository>();

            // 注册核心服务
            services.AddScoped<Interfaces.IJwtService, Services.JwtService>();
            services.AddScoped<Interfaces.ISecurityAuditService, Services.SecurityAuditService>();

            // 注册 MediatR（Application层）
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(Application.Commands.LoginCommand).Assembly));

            // Epic #1731: 注册Auth模块Validators
            services.AddValidatorsFromAssemblyContaining<LYBT.Shared.Models.Validators.Auth.LoginRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<Application.Validators.LoginRequestValidator>();

            return services;
        }

    }
}


