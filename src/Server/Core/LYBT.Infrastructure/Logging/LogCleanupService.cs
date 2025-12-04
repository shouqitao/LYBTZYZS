using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Logging;

/// <summary>
/// 日志清理后台服务
/// refactor-logging-system: 定期清理过期的数据库日志，防止无限增长
/// </summary>
public class LogCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly LogCleanupOptions _options;

    public LogCleanupService(
        IServiceProvider serviceProvider,
        ILogger<LogCleanupService> logger,
        IOptions<LogCleanupOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("日志清理服务已禁用");
            return;
        }

        _logger.LogInformation(
            "日志清理服务已启动 - 保留天数: {RetentionDays}, 清理间隔: {IntervalHours}小时",
            _options.RetentionDays,
            _options.CleanupIntervalHours);

        // 初始延迟，避免启动时立即执行
        await Task.Delay(TimeSpan.FromMinutes(_options.InitialDelayMinutes), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldLogsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志清理作业执行失败");
            }

            // 等待下一次清理
            await Task.Delay(TimeSpan.FromHours(_options.CleanupIntervalHours), stoppingToken);
        }
    }

    private async Task CleanupOldLogsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);

        _logger.LogInformation(
            "开始清理日志 - 截止日期: {CutoffDate:yyyy-MM-dd HH:mm:ss}",
            cutoffDate);

        try
        {
            // 分批删除以避免长时间锁表
            var totalDeleted = 0;
            int deletedInBatch;

            do
            {
                // 使用原生SQL进行批量删除，提高效率
                // V1.0.0: Error/Fatal级别日志永久保留，仅清理Warning及以下级别
                deletedInBatch = await dbContext.Database.ExecuteSqlRawAsync(
                    "DELETE TOP (@batchSize) FROM SystemLogs WHERE Timestamp < @cutoffDate AND Level NOT IN ('Error', 'Fatal')",
                    new Microsoft.Data.SqlClient.SqlParameter("@batchSize", _options.BatchSize),
                    new Microsoft.Data.SqlClient.SqlParameter("@cutoffDate", cutoffDate));

                totalDeleted += deletedInBatch;

                if (deletedInBatch > 0)
                {
                    _logger.LogDebug("已删除 {Count} 条日志记录", deletedInBatch);

                    // 短暂延迟，减少数据库压力
                    await Task.Delay(100, cancellationToken);
                }

            } while (deletedInBatch == _options.BatchSize && !cancellationToken.IsCancellationRequested);

            _logger.LogInformation(
                "日志清理完成 - 共删除 {TotalDeleted} 条过期记录",
                totalDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "日志清理过程中发生错误");
            throw;
        }
    }
}

/// <summary>
/// 日志清理配置选项
/// </summary>
public class LogCleanupOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Lybt:Logging:Cleanup";

    /// <summary>
    /// 是否启用日志清理（默认true）
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 日志保留天数（默认90天）
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// 清理间隔（小时，默认24小时）
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;

    /// <summary>
    /// 初始延迟（分钟，默认5分钟，避免启动时立即执行）
    /// </summary>
    public int InitialDelayMinutes { get; set; } = 5;

    /// <summary>
    /// 批量删除大小（默认1000条/批）
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}
