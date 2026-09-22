using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// 备份计划调度（B-06：备份计划调度）。
/// </summary>
/// <remarks>
/// <para>仅在 <c>Backup:AutoBackup:Enabled=true</c> 时启动；默认关闭，由部署侧按需开启
/// （服务端每日自动全量备份：NFR-AVAIL-001）。</para>
/// <para>循环内调用 <see cref="IBackupService.AutoBackupAsync"/>——该方法自带间隔判定，
/// 未到间隔则为空操作，因此检查周期短于备份间隔也不会产生重复备份。</para>
/// <para>登录触发的自动备份（NFR-AVAIL-001「登录成功后自动备份」）不经本服务，由
/// <c>POST /api/v1/backup/auto</c> 端点承担（同一间隔判定），避免宿主启动即备份。</para>
/// </remarks>
public sealed class BackupSchedulerService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

    private readonly IServiceProvider _services;
    private readonly IOptions<BackupOptions> _options;
    private readonly ILogger<BackupSchedulerService> _logger;

    public BackupSchedulerService(
        IServiceProvider services,
        IOptions<BackupOptions> options,
        ILogger<BackupSchedulerService> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.AutoBackup.Enabled)
        {
            _logger.LogInformation("[BACKUP] 计划调度备份未启用（Backup:AutoBackup:Enabled=false）");
            return;
        }

        _logger.LogInformation(
            "[BACKUP] 计划调度备份已启用：间隔 {Interval} 小时，保留 {Retention} 天，类型 {Kind}",
            options.AutoBackup.IntervalHours,
            options.RetentionDays,
            options.AutoBackup.Kind);

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(options.AutoBackup.InitialDelayMinutes), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var backupService = _services.GetRequiredService<IBackupService>();
                var result = await backupService.AutoBackupAsync(stoppingToken);
                if (result.Success && result.AffectedCount > 0)
                    _logger.LogInformation("[BACKUP] 计划调度备份完成：{Message}", result.Message);
                else if (!result.Success)
                    _logger.LogWarning("[BACKUP] 计划调度备份失败：{Error}", result.Error);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BACKUP] 计划调度备份异常");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
