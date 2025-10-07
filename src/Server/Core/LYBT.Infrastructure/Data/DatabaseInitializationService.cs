using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Data;

/// <summary>
/// 简化版数据库初始化服务
/// 采用最小化设计原则，仅保留必要功能
/// </summary>
public class DatabaseInitializationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        AppDbContext context,
        ILogger<DatabaseInitializationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 初始化数据库 - 使用 EF Migrations
    /// </summary>
    /// <summary>
    /// 初始化数据库 - 使用 EF Migrations
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("开始初始化数据库并应用迁移");

            // 检查是否为关系型数据库（排除 InMemory 数据库）
            if (_context.Database.IsRelational())
            {
                // 幂等性检查：先检查是否有待应用的 Migration
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();

                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("发现 {Count} 个待应用的迁移: {Migrations}",
                        pendingMigrations.Count(),
                        string.Join(", ", pendingMigrations));

                    // 重试逻辑：最多重试 3 次，每次等待 1-3 秒（随机退避）
                    var retryCount = 0;
                    var maxRetries = 3;
                    var random = new Random();

                    while (retryCount < maxRetries)
                    {
                        try
                        {
                            await _context.Database.MigrateAsync();
                            _logger.LogInformation("数据库迁移应用成功");
                            break;
                        }
                        catch (Exception ex) when (retryCount < maxRetries - 1)
                        {
                            retryCount++;
                            var delaySeconds = random.Next(1, 4); // 1-3秒随机延迟
                            _logger.LogWarning(ex, "迁移失败，第 {Retry}/{MaxRetries} 次重试，等待 {Delay}秒后重试...",
                                retryCount, maxRetries, delaySeconds);
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("无待应用的迁移，数据库已是最新状态");
                }

                _logger.LogInformation("数据库初始化完成");
            }
            else
            {
                // InMemory 或其他非关系型数据库，确保数据库已创建
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("数据库初始化完成（InMemory 数据库）");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 获取数据库信息 - 简化版本
    /// </summary>
    public async Task<string> GetDatabaseInfoAsync()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            return canConnect ? "数据库连接正常" : "数据库连接失败";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据库信息失败");
            return "数据库状态未知";
        }
    }
}
