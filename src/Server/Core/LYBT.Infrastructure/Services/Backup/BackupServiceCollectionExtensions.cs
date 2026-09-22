using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// 备份服务注册（双宿主共用：远程 WebAPI 与桌面内嵌 LocalWebAPI）。
/// </summary>
public static class BackupServiceCollectionExtensions
{
    /// <summary>
    /// 注册备份引擎、进度跟踪器与计划调度服务。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置（读取 <c>Backup</c> 节）</param>
    /// <param name="defaultBackupDirectory">未配置 <c>Backup:Directory</c> 时的宿主默认备份目录</param>
    public static IServiceCollection AddBackupServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string defaultBackupDirectory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultBackupDirectory);

        services
            .AddOptions<BackupOptions>()
            .Bind(configuration.GetSection(BackupOptions.SectionName))
            .Configure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.Directory))
                    options.Directory = defaultBackupDirectory;
            })
            .ValidateDataAnnotations();

        services.AddSingleton<BackupJobTracker>();
        services.AddSingleton<IBackupService, SqlServerBackupService>();
        services.AddHostedService<BackupSchedulerService>();

        return services;
    }
}
