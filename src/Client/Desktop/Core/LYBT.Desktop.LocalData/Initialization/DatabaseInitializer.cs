using LYBT.Desktop.LocalData.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Initialization;

/// <summary>
/// 数据库初始化器 - 负责创建 SQL Server LocalDB 数据库和初始化数据
/// OpenSpec: implement-local-mode
/// </summary>
public class DatabaseInitializer
{
    private readonly LocalDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(LocalDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            // 创建数据库（如果不存在）-- SQL Server LocalDB 自动管理数据文件
            var created = await _context.Database.EnsureCreatedAsync(ct);
            if (created)
            {
                _logger.LogInformation("[LocalData] SQL Server LocalDB 数据库已创建");
            }
            else
            {
                _logger.LogDebug("[LocalData] SQL Server LocalDB 数据库已存在");
            }

            // 始终执行种子数据 - SeedData 内部已处理幂等性
            await SeedData.SeedAsync(_context, _logger, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LocalData] 数据库初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 检查数据库是否可连接
    /// </summary>
    public async Task<bool> CanConnectAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(ct);
        }
        catch
        {
            return false;
        }
    }
}
