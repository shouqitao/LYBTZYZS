using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LYBT.WebAPI.BackgroundServices;

/// <summary>
/// 安全审计日志清理后台服务
/// Issue #1873 - 每日凌晨3点清理30天前的审计日志
/// </summary>
public class SecurityAuditCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecurityAuditCleanupService> _logger;

    public SecurityAuditCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<SecurityAuditCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 后台服务执行方法
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SecurityAuditCleanupService已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 计算到下一个凌晨3点的延迟时间
                var now = DateTime.Now;
                var next3AM = DateTime.Today.AddDays(1).AddHours(3);

                // 如果当前时间还未到今天的凌晨3点，则调整为今天的凌晨3点
                if (now.Hour < 3)
                {
                    next3AM = DateTime.Today.AddHours(3);
                }

                var delay = next3AM - now;

                _logger.LogInformation("下一次清理计划时间：{NextCleanupTime}，等待：{Delay}",
                    next3AM, delay);

                // 等待到凌晨3点（或被取消）
                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                // 执行清理
                if (!stoppingToken.IsCancellationRequested)
                {
                    await CleanupOldLogsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止，不记录错误
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecurityAuditCleanupService执行过程中发生错误");
                // 发生错误后等待1小时再重试
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("SecurityAuditCleanupService已停止");
    }

    /// <summary>
    /// 清理30天前的审计日志
    /// </summary>
    private async Task CleanupOldLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("开始清理旧的审计日志...");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 计算截止日期（30天前）
            var cutoffDate = DateTime.UtcNow.AddDays(-30);

            // 查询30天前的日志
            var oldLogs = await context.SecurityAuditLogs
                .Where(log => log.CreatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldLogs.Any())
            {
                context.SecurityAuditLogs.RemoveRange(oldLogs);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("清理了{Count}条审计日志（{CutoffDate}之前）",
                    oldLogs.Count, cutoffDate);
            }
            else
            {
                _logger.LogInformation("没有需要清理的审计日志");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理审计日志时发生错误");
            // 不抛出异常，避免影响后续定时执行
        }
    }
}
