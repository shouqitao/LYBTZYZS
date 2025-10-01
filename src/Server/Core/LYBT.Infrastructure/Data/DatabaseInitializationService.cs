using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;

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
    public async Task InitializeDatabaseAsync()
    {
        try
    {
            _logger.LogInformation("开始初始化数据库并应用迁移");

            // 使用 Migrations 自动应用待执行的迁移
            await _context.Database.MigrateAsync();

            _logger.LogInformation("数据库初始化完成，所有迁移已应用");
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