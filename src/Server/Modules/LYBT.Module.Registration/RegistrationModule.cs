using FluentValidation;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Validation;
using LYBT.Infrastructure.Data;
using LYBT.Module.Registrations.Application.Commands;
using LYBT.Module.Registrations.Application.Validators;
using LYBT.Module.Registrations.Hubs;
using LYBT.Module.Registrations.Infrastructure;
using LYBT.Module.Registrations.Interfaces;
using LYBT.Module.Registrations.Mappers;
using LYBT.Module.Registrations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Registrations;

/// <summary>
/// 挂号模块服务注册
/// </summary>
public static class RegistrationModule
{
    /// <summary>
    /// 注册挂号模块服务
    /// </summary>
    public static IServiceCollection AddRegistrationModule(this IServiceCollection services)
    {
        // 注册仓储（使用AppDbContext）
        services.AddScoped<IRegistrationRepository, Infrastructure.RegistrationRepository>();

        // 注册跨模块服务
        services.AddScoped<IRegistrationCrossModuleService, RegistrationCrossModuleService>();

        // 注册 Mapper
        services.AddSingleton<RegistrationMapper>();

        // 注册 MediatR（Application层）
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateRegistrationCommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // 注册 Application 层验证器
        services.AddValidatorsFromAssemblyContaining<CreateRegistrationValidator>();

        // 注册 SignalR 实时通知 (US-REG-008)
        services.AddSingleton<RegistrationConnectionManager>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}


