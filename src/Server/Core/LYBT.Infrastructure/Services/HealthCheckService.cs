using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HealthStatus = LYBT.Shared.Models.Contracts.Health.HealthStatus;

namespace LYBT.Infrastructure.Services
{
    /// <summary>
    /// 健康检查服务实现
    /// 将健康检查逻辑从Controller移至Service层，遵循三层架构
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<HealthCheckService> _logger;

        public HealthCheckService(IDbContextAccessor dbAccessor, ILogger<HealthCheckService> logger)
        {
            _dbContext = dbAccessor.Context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<DatabaseHealthCheckResult> CheckDatabaseAsync()
        {
            var result = new DatabaseHealthCheckResult("db", "Database Connectivity");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    result.Status = HealthStatus.Unhealthy;
                    return result;
                }

                // 获取数据库 Provider 名称
                result.Provider = _dbContext.Database.ProviderName;

                // 检查是否为关系型数据库（排除 InMemory 数据库）
                var isRelationalDatabase = _dbContext.Database.IsRelational();

                if (isRelationalDatabase)
                {
                    // 获取服务器版本信息
                    try
                    {
                        var connection = _dbContext.Database.GetDbConnection();
                        if (connection.State == System.Data.ConnectionState.Open)
                        {
                            result.ServerVersion = connection.ServerVersion;
                        }
                    }
                    catch
                    {
                        // ServerVersion 获取失败不影响健康检查
                        _logger.LogDebug("Failed to get database server version");
                    }

                    // 仅在关系型数据库上检查迁移
                    var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                    var pendingCount = pendingMigrations.Count();
                    result.PendingMigrationCount = pendingCount;

                    result.Status = pendingCount == 0 ? HealthStatus.Healthy : HealthStatus.Degraded;
                }
                else
                {
                    // InMemory 或其他非关系型数据库
                    result.Status = HealthStatus.Healthy;
                }
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Unhealthy;
                _logger.LogError(ex, "Database health check failed");
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<HealthStatus> GetOverallStatusAsync()
        {
            var dbCheck = await CheckDatabaseAsync();
            return dbCheck.Status;
        }
    }
}
