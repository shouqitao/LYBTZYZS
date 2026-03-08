using LYBT.Module.Registration.Interfaces;
using LYBT.Module.Registration.Repositories;
using LYBT.Module.Registration.Services;
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
    public static IServiceCollection AddRegistrationModule(this IServiceCollection services)
    {
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();
        services.AddScoped<IRegistrationService, RegistrationService>();

        return services;
    }
}
