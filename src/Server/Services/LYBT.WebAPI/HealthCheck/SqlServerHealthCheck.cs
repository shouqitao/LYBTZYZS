using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.WebAPI.HealthCheck;

/// <summary>
/// SQL Server健康检查实现
/// Issue #1726 Phase 3 - 提供 /health/database 端点
/// </summary>
public class SqlServerHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public SqlServerHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Unhealthy(
                    "连接字符串未配置",
                    data: new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "ConfigKey", "ConnectionStrings:DefaultConnection" }
                    });
            }

            // 解析连接信息
            var builder = new SqlConnectionStringBuilder(connectionString);
            var serverName = builder.DataSource;
            var databaseName = builder.InitialCatalog;

            // 测试连接（5秒超时）
            using (var connection = new SqlConnection(connectionString))
            {
                var timeout = TimeSpan.FromSeconds(5);
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    cts.CancelAfter(timeout);

                    await connection.OpenAsync(cts.Token);

                    // 验证数据库可访问
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT 1";
                        await command.ExecuteScalarAsync(cts.Token);
                    }

                    return HealthCheckResult.Healthy(
                        "SQL Server连接正常",
                        data: new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "Server", serverName },
                            { "Database", databaseName },
                            { "ResponseTime", $"< {timeout.TotalSeconds}s" }
                        });
                }
            }
        }
        catch (SqlException ex)
        {
            return HealthCheckResult.Unhealthy(
                $"SQL Server连接失败: {ex.Message}",
                exception: ex,
                data: new System.Collections.Generic.Dictionary<string, object>
                {
                    { "ErrorCode", ex.Number },
                    { "SqlState", ex.State },
                    { "Suggestion", GetSuggestion(ex.Number) }
                });
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "SQL Server连接超时（>5秒）");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"数据库健康检查失败: {ex.Message}",
                exception: ex);
        }
    }

    /// <summary>
    /// 根据SQL错误代码提供故障排查建议
    /// </summary>
    private string GetSuggestion(int errorCode)
    {
        return errorCode switch
        {
            -1 or 2 => "SQL Server服务未启动，请检查服务状态",
            4060 => "数据库不存在，请运行数据库迁移",
            18456 => "Windows Authentication权限不足，请检查用户权限",
            _ => "请检查连接字符串和网络连接"
        };
    }
}
