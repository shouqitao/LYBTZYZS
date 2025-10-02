using System.Data.SqlClient;
using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Configuration
{
    /// <summary>
    /// SQL Server测试数据库上下文工厂
    /// 符合PRD要求：使用SQL Server（非LocalDB）进行测试
    /// </summary>
    public class SqlServerTestDbContextFactory : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SqlServerTestDbContextFactory> _logger;
        private readonly string _testDatabaseName;
        private AppDbContext? _context;

        public SqlServerTestDbContextFactory(IConfiguration configuration, ILogger<SqlServerTestDbContextFactory> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _testDatabaseName = $"LYBTDB_Test_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// 创建测试数据库上下文
        /// </summary>
        public async Task<AppDbContext> CreateContextAsync()
        {
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("未找到SQL Server连接字符串配置");
            }

            // 替换数据库名称为测试专用数据库
            var testConnectionString = connectionString.Replace("LYBTDB_Test", _testDatabaseName);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(testConnectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                })
                .EnableSensitiveDataLogging(false)
                .EnableDetailedErrors(false)
                .Options;

            _context = new AppDbContext(options);

            try
            {
                // 确保数据库存在
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("测试数据库已创建: {DatabaseName}", _testDatabaseName);

                return _context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建测试数据库失败: {DatabaseName}", _testDatabaseName);
                _context?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 清理测试数据库
        /// </summary>
        public async Task CleanupAsync()
        {
            if (_context != null)
            {
                try
                {
                    await _context.Database.EnsureDeletedAsync();
                    _logger.LogInformation("测试数据库已删除: {DatabaseName}", _testDatabaseName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除测试数据库失败: {DatabaseName}", _testDatabaseName);
                }
                finally
                {
                    _context.Dispose();
                    _context = null;
                }
            }
        }

        /// <summary>
        /// 验证SQL Server连接
        /// </summary>
        public async Task<bool> VerifySqlServerConnectionAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("SqlServerConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return false;
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("SQL Server连接验证成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Server连接验证失败");
                return false;
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
