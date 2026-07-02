using FluentValidation;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Validators.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
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
            services.AddDbContext<Infrastructure.AuthDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // 注册仓储
            services.AddScoped<Interfaces.IAuthSessionRepository, Infrastructure.AuthSessionRepository>();

            // 注册核心服务
            services.AddSingleton<Interfaces.IJwtService, Services.JwtService>();

            // 注册 MediatR（Application层）
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(Application.Commands.LoginCommand).Assembly));

            // Epic #1731: 注册Auth模块Validators
            services.AddValidatorsFromAssemblyContaining<Shared.Validators.Auth.LoginRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<Application.Validators.LoginRequestValidator>();

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


