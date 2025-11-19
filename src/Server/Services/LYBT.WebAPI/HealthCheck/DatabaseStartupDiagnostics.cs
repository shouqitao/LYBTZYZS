using Microsoft.Data.SqlClient;

namespace LYBT.WebAPI.HealthCheck;

/// <summary>
/// 数据库启动诊断服务
/// Issue #1726 Phase 3 - SQL Server健康检查增强
/// </summary>
public class DatabaseStartupDiagnostics : IHostedService
{
    private readonly ILogger<DatabaseStartupDiagnostics> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseStartupDiagnostics(
        ILogger<DatabaseStartupDiagnostics> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(" [DatabaseStartupDiagnostics] 开始数据库连接诊断...");

        try
        {
            // 1. 读取连接字符串
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError(" [DatabaseStartupDiagnostics] 未找到连接字符串 'ConnectionStrings:DefaultConnection'");
                return;
            }

            _logger.LogInformation(" [DatabaseStartupDiagnostics] 连接字符串已加载");

            // 2. 解析连接字符串
            var builder = new SqlConnectionStringBuilder(connectionString);
            var serverName = builder.DataSource;
            var databaseName = builder.InitialCatalog;
            var useWindowsAuth = builder.IntegratedSecurity;

            _logger.LogInformation(" [DatabaseStartupDiagnostics] 连接信息:");
            _logger.LogInformation($"   - 服务器: {serverName}");
            _logger.LogInformation($"   - 数据库: {databaseName}");
            _logger.LogInformation($"   - 认证方式: {(useWindowsAuth ? "Windows Authentication" : "SQL Server Authentication")}");

            // 3. 测试连接
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation(" [DatabaseStartupDiagnostics] 数据库连接成功！");

                // 4. 验证数据库存在
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT DB_NAME()";
                    var result = await command.ExecuteScalarAsync(cancellationToken);
                    _logger.LogInformation($" [DatabaseStartupDiagnostics] 当前数据库: {result}");
                }

                // 5. 检查连接池状态
                _logger.LogInformation($" [DatabaseStartupDiagnostics] 连接池配置:");
                _logger.LogInformation($"   - Max Pool Size: {builder.MaxPoolSize}");
                _logger.LogInformation($"   - Min Pool Size: {builder.MinPoolSize}");
                _logger.LogInformation($"   - Connection Timeout: {builder.ConnectTimeout}秒");
            }

            _logger.LogInformation(" [DatabaseStartupDiagnostics] 数据库诊断完成，系统可正常启动");
        }
        catch (SqlException ex)
        {
            _logger.LogError(" [DatabaseStartupDiagnostics] SQL Server连接失败！");
            _logger.LogError($"   错误代码: {ex.Number}");
            _logger.LogError($"   错误信息: {ex.Message}");

            // 提供详细的故障排查建议
            _logger.LogWarning(" [DatabaseStartupDiagnostics] 故障排查建议:");

            if (ex.Number == -1 || ex.Number == 2)
            {
                _logger.LogWarning("   1. 检查SQL Server服务是否启动（服务名: MSSQLSERVER 或 MSSQL$SQLEXPRESS）");
                _logger.LogWarning("      PowerShell命令: Get-Service -Name 'MSSQL$SQLEXPRESS'");
            }
            else if (ex.Number == 4060)
            {
                _logger.LogWarning("   1. 数据库不存在，请检查数据库名称是否正确");
                _logger.LogWarning("   2. 或运行数据库迁移命令创建数据库");
            }
            else if (ex.Number == 18456)
            {
                _logger.LogWarning("   1. Windows Authentication权限不足");
                _logger.LogWarning("   2. 检查当前Windows用户是否有SQL Server访问权限");
            }

            _logger.LogWarning("   通用检查:");
            _logger.LogWarning("   - 验证连接字符串配置是否正确");
            _logger.LogWarning("   - 确认防火墙允许SQL Server端口（默认1433）");

            //  不抛出异常，允许应用继续启动（可稍后手动修复数据库）
            _logger.LogWarning(" [DatabaseStartupDiagnostics] 应用将继续启动，但数据库功能不可用");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " [DatabaseStartupDiagnostics] 数据库诊断过程中发生未知错误");
            _logger.LogWarning(" [DatabaseStartupDiagnostics] 应用将继续启动，但数据库功能不可用");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 [DatabaseStartupDiagnostics] 数据库诊断服务停止");
        return Task.CompletedTask;
    }
}
