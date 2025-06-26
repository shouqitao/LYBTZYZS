namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 所有模块服务注入
/// </summary>
public static class ServiceCollectionExtension {

    public static IServiceCollection AddLybtModules(this IServiceCollection services) {
        services.AddSingleton<IUserService, UserService>();
        // 注册更多模块...
        return services;
    }
}