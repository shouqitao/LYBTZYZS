using LYBT.Infrastructure.Data;
using LYBT.Entities.Users;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Data
{

    /// <summary>
    /// 数据库初始化服务
    /// 负责在应用启动时检查和初始化数据库
    /// </summary>
    public class DatabaseInitializationService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DatabaseInitializationService> _logger;
        private readonly IConfiguration _configuration;

        public DatabaseInitializationService(
            AppDbContext dbContext, 
            ILogger<DatabaseInitializationService> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
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

                // 5. 初始化默认管理员密码
                await InitializeAdminSecretsAsync();

                _logger.LogInformation("✅ 数据库初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 数据库初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 检查数据库服务器连接和数据库可用性
        /// </summary>
        private async Task CheckDatabaseConnectionAsync()
        {
            try
            {
                _logger.LogInformation("检查数据库服务器连接...");

                // 首先检查SQL Server服务器是否可用（连接到master数据库）
                await CheckSqlServerAvailabilityAsync();

                // 然后检查目标数据库是否存在和可访问
                await CheckTargetDatabaseAsync();

                _logger.LogInformation("✅ 数据库连接检查完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 数据库连接检查失败");
                throw;
            }
        }

        /// <summary>
        /// 检查SQL Server服务器是否可用
        /// </summary>
        private async Task CheckSqlServerAvailabilityAsync()
        {
            try
            {
                // 构建连接到master数据库的连接字符串来测试SQL Server可用性
                var connectionString = _dbContext.Database.GetConnectionString();
                var masterConnectionString = connectionString?.Replace("Database=LYBTDB", "Database=master");

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(masterConnectionString);

                _logger.LogInformation("正在连接到SQL Server (master数据库)...");

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await connection.OpenAsync(timeout.Token);

                _logger.LogInformation("✅ SQL Server服务器连接成功");

                // 检查服务器版本信息
                using var command = new Microsoft.Data.SqlClient.SqlCommand("SELECT @@VERSION", connection);
                var version = await command.ExecuteScalarAsync() as string;
                if (!string.IsNullOrEmpty(version))
                {
                    // 只显示版本信息的第一行
                    var versionFirstLine = version.Split('\n')[0].Trim();
                    _logger.LogInformation($"SQL Server版本: {versionFirstLine}");
                }
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 2 || sqlEx.Number == 53)
            {
                // 连接超时或服务器不可达
                _logger.LogError("❌ 无法连接到SQL Server服务器");
                _logger.LogError("可能的原因:");
                _logger.LogError("  1. SQL Server服务未启动");
                _logger.LogError("  2. SQL Server未安装");
                _logger.LogError("  3. 服务器名称不正确");
                _logger.LogError("  4. 防火墙阻止连接");
                _logger.LogError("");
                _logger.LogError("解决建议:");
                _logger.LogError("  1. 安装SQL Server Express: https://www.microsoft.com/sql-server/sql-server-downloads");
                _logger.LogError("  2. 启动SQL Server服务: services.msc -> SQL Server");
                _logger.LogError("  3. 检查连接字符串配置");

                throw new InvalidOperationException($"SQL Server服务器不可用: {sqlEx.Message}", sqlEx);
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 18456)
            {
                // 身份验证失败
                _logger.LogError("❌ SQL Server身份验证失败");
                _logger.LogError("当前连接使用Windows集成身份验证");
                _logger.LogError($"当前用户: {Environment.UserName}");
                _logger.LogError("");
                _logger.LogError("解决建议:");
                _logger.LogError("  1. 确保当前用户有SQL Server访问权限");
                _logger.LogError("  2. 或修改连接字符串使用SQL Server身份验证");

                throw new InvalidOperationException($"数据库身份验证失败: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ SQL Server连接测试失败");
                throw new InvalidOperationException("无法连接到SQL Server服务器，请检查服务器状态", ex);
            }
        }

        /// <summary>
        /// 检查目标数据库是否存在，如果不存在则创建
        /// </summary>
        private async Task CheckTargetDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("检查目标数据库 LYBTDB...");

                // 尝试连接到目标数据库
                var canConnect = await _dbContext.Database.CanConnectAsync();

                if (canConnect)
                {
                    _logger.LogInformation("✅ 数据库 LYBTDB 存在且可访问");
                }
                else
                {
                    _logger.LogInformation("数据库 LYBTDB 不存在，正在创建...");
                    await CreateDatabaseIfNotExistsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查目标数据库时出现问题，尝试创建数据库...");
                await CreateDatabaseIfNotExistsAsync();
            }
        }

        /// <summary>
        /// 如果数据库不存在则创建数据库
        /// </summary>
        private async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                _logger.LogInformation("正在创建数据库 LYBTDB...");

                // 使用Migrate方法创建数据库并应用所有迁移
                // 注意：不要使用EnsureCreated，因为它会绕过迁移系统
                await _dbContext.Database.MigrateAsync();

                _logger.LogInformation("✅ 数据库 LYBTDB 创建成功");
                _logger.LogInformation("✅ 数据库表结构创建成功");

                // 验证数据库连接
                var canConnect = await _dbContext.Database.CanConnectAsync();
                if (canConnect)
                {
                    _logger.LogInformation("✅ 数据库连接验证成功");
                }
                else
                {
                    throw new InvalidOperationException("数据库创建后仍无法连接");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 数据库创建失败");
                throw new InvalidOperationException($"无法创建数据库: {ex.Message}", ex);
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
                // 使用Migrate而不是EnsureCreated
                await _dbContext.Database.MigrateAsync();
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
                var coreTableNames = new[] { "Users", "AdminSecrets", "AuditLogs", "Patients" };

                foreach (var tableName in coreTableNames)
                {
                    try
                    {
                        // 尝试查询表以验证其存在 - 使用 FromSqlRaw 而不是 ExecuteSqlRawAsync
                        var sql = $"SELECT TOP 0 * FROM [{tableName}]";
                        var result = await _dbContext.Database.ExecuteSqlRawAsync(sql);
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

        /// <summary>
        /// 初始化AdminSecrets表默认数据
        /// </summary>
        private async Task InitializeAdminSecretsAsync()
        {
            try
            {
                _logger.LogInformation("检查AdminSecrets表初始化状态...");

                // 先检查表是否存在
                try
                {
                    var sql = "SELECT TOP 1 1 FROM AdminSecrets";
                    await _dbContext.Database.ExecuteSqlRawAsync(sql);
                }
                catch (Exception tableEx)
                {
                    _logger.LogWarning($"AdminSecrets表可能不存在: {tableEx.Message}");
                    // 如果表不存在，让EF Core的迁移处理它
                    return;
                }

                // 检查是否已存在sysadmin记录
                var existingAdmin = await _dbContext.AdminSecrets
                    .FirstOrDefaultAsync(x => x.Username == "sysadmin");

                if (existingAdmin == null)
                {
                    _logger.LogInformation("正在创建默认超级管理员密码...");

                    // 从配置读取默认密码
                    var defaultPassword = _configuration.GetValue<string>("SysAdminOptions:DefaultPassword") ?? "Admin@123456";
                    var passwordHash = PasswordHelper.Hash(defaultPassword);

                    // 创建AdminSecret记录
                    var adminSecret = new AdminSecretModel
                    {
                        Id = Guid.NewGuid(),
                        Username = "sysadmin",
                        PasswordHash = passwordHash
                    };

                    _dbContext.AdminSecrets.Add(adminSecret);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("✅ 默认超级管理员密码已创建");
                    _logger.LogInformation($"默认登录信息: 用户名=sysadmin, 密码={defaultPassword}");
                }
                else
                {
                    _logger.LogInformation("✅ AdminSecrets表已存在超级管理员记录");

                    // 不再自动更新密码哈希，避免覆盖用户修改的密码
                    // 如果需要重置密码，应该通过专门的管理功能进行
                    _logger.LogDebug($"超级管理员 sysadmin 已存在，ID: {existingAdmin.Id}");
                }
            }
            catch (Exception ex)
            {
                // 将此错误降级为警告，不影响系统启动
                _logger.LogWarning(ex, "⚠️ 初始化AdminSecrets表时出现问题，但不影响系统启动");
                // 不再抛出异常，让系统继续启动
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