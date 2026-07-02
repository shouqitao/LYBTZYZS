using FluentValidation;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Data;
using LYBT.Module.Registration.Application.Commands;
using LYBT.Module.Registration.Application.Validators;
using LYBT.Module.Registration.Infrastructure;
using LYBT.Module.Registration.Interfaces;
using LYBT.Module.Registration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Registration;

/// <summary>
/// 挂号模块服务注册
/// </summary>
public static class RegistrationModule
{
    /// <summary>
    /// 注册挂号模块服务
    /// </summary>
    public static IServiceCollection AddRegistrationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册仓储（使用AppDbContext）
        services.AddScoped<IRegistrationRepository, Infrastructure.RegistrationRepository>();

        // 注册跨模块服务
        services.AddScoped<IRegistrationCrossModuleService, RegistrationCrossModuleService>();

        // 注册 MediatR（Application层）
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateRegistrationCommand).Assembly));

        // 注册 Application 层验证器
        services.AddValidatorsFromAssemblyContaining<CreateRegistrationValidator>();

        return services;
    }
}


