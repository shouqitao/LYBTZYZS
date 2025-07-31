using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace LYBT.Infrastructure.Database
{
    /// <summary>
    /// 数据库初始化服务
    /// 负责在应用启动时检查和初始化数据库
    /// </summary>
    public class DatabaseInitializationService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(AppDbContext dbContext, ILogger<DatabaseInitializationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("开始数据库初始化检查...");

                // 1. 检查数据库连接
                await CheckDatabaseConnectionAsync();

                // 2. 检查数据库是否存在
                var databaseExists = await CheckDatabaseExistsAsync();
                
                if (!databaseExists)
                {
                    _logger.LogInformation("数据库不存在，正在创建数据库...");
                    await CreateDatabaseAsync();
                }

                // 3. 检查并应用待处理的迁移
                await CheckAndApplyMigrationsAsync();

                // 4. 验证数据库表结构
                await ValidateDatabaseSchemaAsync();

                _logger.LogInformation("✅ 数据库初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 数据库初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 检查数据库连接
        /// </summary>
        private async Task CheckDatabaseConnectionAsync()
        {
            try
            {
                _logger.LogInformation("检查数据库连接...");
                
                // 设置连接超时时间
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _dbContext.Database.OpenConnectionAsync(cancellationTokenSource.Token);
                await _dbContext.Database.CloseConnectionAsync();
                
                _logger.LogInformation("✅ 数据库连接正常");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 数据库连接失败");
                throw new InvalidOperationException("无法连接到数据库服务器，请检查连接字符串和数据库服务器状态", ex);
            }
        }

        /// <summary>
        /// 检查数据库是否存在
        /// </summary>
        private async Task<bool> CheckDatabaseExistsAsync()
        {
            try
            {
                var exists = await _dbContext.Database.CanConnectAsync();
                _logger.LogInformation($"数据库存在状态: {(exists ? "存在" : "不存在")}");
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查数据库存在状态时出现异常，假定数据库不存在");
                return false;
            }
        }

        /// <summary>
        /// 创建数据库
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("正在创建数据库...");
                await _dbContext.Database.EnsureCreatedAsync();
                _logger.LogInformation("✅ 数据库创建成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 数据库创建失败");
                throw;
            }
        }

        /// <summary>
        /// 检查并应用待处理的迁移
        /// </summary>
        private async Task CheckAndApplyMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("检查数据库迁移状态...");

                // 获取待处理的迁移
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                var pendingMigrationsList = pendingMigrations.ToList();

                if (pendingMigrationsList.Any())
                {
                    _logger.LogInformation($"发现 {pendingMigrationsList.Count} 个待处理的迁移:");
                    foreach (var migration in pendingMigrationsList)
                    {
                        _logger.LogInformation($"  - {migration}");
                    }

                    _logger.LogInformation("正在应用数据库迁移...");
                    await _dbContext.Database.MigrateAsync();
                    _logger.LogInformation("✅ 数据库迁移应用成功");
                }
                else
                {
                    _logger.LogInformation("✅ 数据库已是最新版本，无需迁移");
                }

                // 显示已应用的迁移历史
                var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync();
                var appliedMigrationsList = appliedMigrations.ToList();
                
                if (appliedMigrationsList.Any())
                {
                    _logger.LogInformation($"已应用的迁移数量: {appliedMigrationsList.Count}");
                    _logger.LogDebug("已应用的迁移列表:");
                    foreach (var migration in appliedMigrationsList)
                    {
                        _logger.LogDebug($"  - {migration}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 数据库迁移失败");
                throw;
            }
        }

        /// <summary>
        /// 验证数据库表结构
        /// </summary>
        private async Task ValidateDatabaseSchemaAsync()
        {
            try
            {
                _logger.LogInformation("验证数据库表结构...");

                // 检查关键表是否存在
                var coreTableNames = new[] { "Users", "UserRoles", "SystemConfigs", "AuditLogs" };
                
                foreach (var tableName in coreTableNames)
                {
                    try
                    {
                        // 尝试查询表以验证其存在
                        var sql = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
                        var result = await _dbContext.Database.ExecuteSqlRawAsync($"SELECT TOP 0 * FROM [{tableName}]");
                        _logger.LogDebug($"✅ 表 {tableName} 验证成功");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"⚠️ 表 {tableName} 验证失败: {ex.Message}");
                    }
                }

                _logger.LogInformation("✅ 数据库表结构验证完成");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ 数据库表结构验证出现异常，但不影响程序启动");
            }
        }

        /// <summary>
        /// 获取数据库信息摘要
        /// </summary>
        public async Task<DatabaseInfo> GetDatabaseInfoAsync()
        {
            try
            {
                var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync();
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();

                return new DatabaseInfo
                {
                    IsConnected = await _dbContext.Database.CanConnectAsync(),
                    DatabaseName = _dbContext.Database.GetDbConnection().Database,
                    AppliedMigrationsCount = appliedMigrations.Count(),
                    PendingMigrationsCount = pendingMigrations.Count(),
                    LastMigration = appliedMigrations.LastOrDefault()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取数据库信息失败");
                return new DatabaseInfo
                {
                    IsConnected = false,
                    DatabaseName = "未知",
                    AppliedMigrationsCount = 0,
                    PendingMigrationsCount = 0,
                    LastMigration = null
                };
            }
        }
    }

    /// <summary>
    /// 数据库信息类
    /// </summary>
    public class DatabaseInfo
    {
        public bool IsConnected { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public int AppliedMigrationsCount { get; set; }
        public int PendingMigrationsCount { get; set; }
        public string? LastMigration { get; set; }
    }
}