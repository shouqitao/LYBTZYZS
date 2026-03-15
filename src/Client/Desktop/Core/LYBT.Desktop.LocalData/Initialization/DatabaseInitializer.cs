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
    private readonly Func<LocalDbContext> _contextFactory;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public DatabaseInitializer(Func<LocalDbContext> contextFactory, ILogger<DatabaseInitializer> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 确保数据库已初始化（线程安全，幂等）
    /// </summary>
    public virtual async Task EnsureInitializedAsync(CancellationToken ct = default)
    {
        // 快速检查，避免不必要的锁竞争
        if (_isInitialized)
        {
            _logger.LogDebug("[LocalData] 数据库已初始化，跳过");
            return;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            // 双重检查模式
            if (_isInitialized)
            {
                _logger.LogDebug("[LocalData] 数据库已初始化（锁内检查），跳过");
                return;
            }

            _logger.LogInformation("[LocalData] 开始初始化数据库...");
            await InitializeAsync(ct);
            _isInitialized = true;
            _logger.LogInformation("[LocalData] 数据库初始化完成");
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    private async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            using var context = _contextFactory();

            // 创建数据库（如果不存在）-- SQL Server LocalDB 自动管理数据文件
            var created = await context.Database.EnsureCreatedAsync(ct);
            if (created)
            {
                _logger.LogInformation("[LocalData] SQL Server LocalDB 数据库已创建");
            }
            else
            {
                _logger.LogDebug("[LocalData] SQL Server LocalDB 数据库已存在");
            }

            // 始终执行种子数据 - SeedData 内部已处理幂等性
            await SeedData.SeedAsync(context, _logger, ct);
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
            using var context = _contextFactory();
            return await context.Database.CanConnectAsync(ct);
        }
        catch
        {
            return false;
        }
    }
}
