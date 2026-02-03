using LYBT.Desktop.LocalData.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Initialization;

/// <summary>
/// 数据库初始化器 - 负责创建 SQLite 数据库和初始化数据
/// OpenSpec: implement-local-mode
/// </summary>
public class DatabaseInitializer
{
    private readonly LocalDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    /// <summary>
    /// 数据库文件路径
    /// </summary>
    public static string DatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LYBTZYZS",
        "lybtzyzs.db");

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
            // 确保目录存在
            var directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("[LocalData] 创建数据库目录: {Directory}", directory);
            }

            // 创建数据库（如果不存在）
            var created = await _context.Database.EnsureCreatedAsync(ct);
            if (created)
            {
                _logger.LogInformation("[LocalData] 数据库已创建: {Path}", DatabasePath);

                // 初始化种子数据
                await SeedData.SeedAsync(_context, _logger, ct);
            }
            else
            {
                _logger.LogDebug("[LocalData] 数据库已存在: {Path}", DatabasePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LocalData] 数据库初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 检查数据库是否存在
    /// </summary>
    public static bool DatabaseExists()
    {
        return File.Exists(DatabasePath);
    }

    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    public static string GetConnectionString()
    {
        return $"Data Source={DatabasePath}";
    }
}
