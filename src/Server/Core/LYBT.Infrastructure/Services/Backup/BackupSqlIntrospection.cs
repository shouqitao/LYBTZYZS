using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// SQL Server 备份/恢复所需的**服务器内省与 DDL 辅助**（A-04 职责提取，2026-09-26）。
/// </summary>
/// <remarks>
/// <para>从 <see cref="SqlServerBackupService"/> 抽出：这些方法只依赖连接串与日志，与备份编排（串行化、
/// 进度、清单、加密）无耦合。抽出的目的是把「SQL 内省/DDL」与「备份业务流程」分开，
/// 使内省逻辑可独立阅读与测试。</para>
/// <para><b>行为不变</b>：方法体自 <see cref="SqlServerBackupService"/> 原样迁移，仅把
/// <c>ConnectionString</c>/<c>MasterConnectionString</c> 改为**每次调用时取值**的委托——
/// 保持「连接串可在运行时被配置覆盖」的既有语义（原实现是每次访问属性重新解析）。</para>
/// </remarks>
internal sealed class BackupSqlIntrospection
{
    // 与 SqlServerBackupService 一致：备份/恢复不限时（大库可能远超默认 30s）
    private const int CommandTimeoutSeconds = 0;

    private readonly Func<string> _connectionString;
    private readonly Func<string> _masterConnectionString;
    private readonly ILogger _logger;
    private bool? _supportsBackupCompression;

    internal BackupSqlIntrospection(
        Func<string> connectionString,
        Func<string> masterConnectionString,
        ILogger logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _masterConnectionString = masterConnectionString ?? throw new ArgumentNullException(nameof(masterConnectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>SQL 字面量转义（单引号翻倍）。</summary>
    internal static string EscapeSqlLiteral(string value) => value.Replace("'", "''");

    /// <summary>SQL 标识符转义（右方括号翻倍）。</summary>
    internal static string EscapeIdentifier(string value) => value.Replace("]", "]]");

    /// <summary>可复制的列（选择性恢复用）。</summary>
    internal readonly record struct CopyColumn(string Name, bool IsIdentity);

    /// <summary>禁用/启用全部外键约束（选择性恢复期间临时关闭）。</summary>
    internal async Task SetAllConstraintsAsync(bool disable, CancellationToken ct)
    {
        var action = disable ? "NOCHECK CONSTRAINT ALL" : "WITH NOCHECK CHECK CONSTRAINT ALL";
        var sql = $"""
            DECLARE @sql nvarchar(max) = N'';
            SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' {action};'
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0;
            EXEC sp_executesql @sql;
            """;

        await using var connection = new SqlConnection(_connectionString());
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>读取表可复制的列（排除计算列与 rowversion/timestamp）。</summary>
    internal async Task<List<CopyColumn>> ReadCopyableColumnsAsync(string tableName, CancellationToken ct)
    {
        const string sql = """
            SELECT c.name, c.is_identity, c.is_computed, ty.name AS TypeName
            FROM sys.columns c
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE c.object_id = OBJECT_ID(@qualified)
            ORDER BY c.column_id
            """;

        var columns = new List<CopyColumn>();
        await using var connection = new SqlConnection(_connectionString());
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        command.Parameters.Add(new SqlParameter("@qualified", $"[dbo].[{tableName}]"));
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var isComputed = reader.GetBoolean(2);
            var typeName = reader.GetString(3);
            if (isComputed ||
                string.Equals(typeName, "timestamp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(typeName, "rowversion", StringComparison.OrdinalIgnoreCase))
                continue;

            columns.Add(new CopyColumn(reader.GetString(0), reader.GetBoolean(1)));
        }

        return columns;
    }

    /// <summary>指定数据库内是否存在该表。</summary>
    internal async Task<bool> TableExistsAsync(string tableName, string database, CancellationToken ct)
    {
        var sql = $"SELECT CASE WHEN OBJECT_ID(N'[{EscapeIdentifier(database)}].[dbo].[{EscapeIdentifier(tableName)}]') IS NULL THEN 0 ELSE 1 END";
        await using var connection = new SqlConnection(_connectionString());
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        var value = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(value, CultureInfo.InvariantCulture) == 1;
    }

    /// <summary>读取备份文件的逻辑文件名与类型（RESTORE FILELISTONLY）。</summary>
    internal async Task<List<(string LogicalName, string FileType)>> ReadLogicalFileNamesAsync(string backupPath, CancellationToken ct)
    {
        var sql = $"RESTORE FILELISTONLY FROM DISK = N'{EscapeSqlLiteral(backupPath)}'";
        var files = new List<(string, string)>();
        await using var connection = new SqlConnection(_connectionString());
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
        await using var reader = await command.ExecuteReaderAsync(ct);
        var logicalNameIndex = reader.GetOrdinal("LogicalName");
        var typeIndex = reader.GetOrdinal("Type");
        while (await reader.ReadAsync(ct))
        {
            files.Add((reader.GetString(logicalNameIndex), reader.GetString(typeIndex)));
        }

        return files;
    }

    /// <summary>
    /// 备份压缩能力探测（进程内缓存）：SQL Server Express / LocalDB 不支持
    /// <c>BACKUP DATABASE ... WITH COMPRESSION</c>，请求压缩时须降级为未压缩而非直接失败。
    /// </summary>
    internal async Task<bool> SupportsBackupCompressionAsync(CancellationToken ct)
    {
        if (_supportsBackupCompression.HasValue)
            return _supportsBackupCompression.Value;

        var edition = await ReadServerPropertyAsync("Edition", ct) ?? string.Empty;
        var supported = !edition.Contains("Express", StringComparison.OrdinalIgnoreCase);
        _supportsBackupCompression = supported;
        return supported;
    }

    /// <summary>读取服务器属性（SERVERPROPERTY）。</summary>
    internal async Task<string?> ReadServerPropertyAsync(string property, CancellationToken ct)
    {
        var sql = $"SELECT CAST(SERVERPROPERTY('{EscapeIdentifier(property)}') AS nvarchar(4000))";
        await using var connection = new SqlConnection(_connectionString());
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        var value = await command.ExecuteScalarAsync(ct);
        return value == null || value == DBNull.Value ? null : (string)value;
    }

    /// <summary>若存在则强制删除数据库（选择性恢复的临时库清理）。失败仅告警。</summary>
    internal async Task DropDatabaseIfExistsAsync(string database, CancellationToken ct)
    {
        try
        {
            var sql = $"""
                IF DB_ID(N'{EscapeSqlLiteral(database)}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{database}];
                END
                """;
            await using var connection = new SqlConnection(_connectionString());
            await connection.OpenAsync(ct);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BACKUP] 清理临时库失败：{Database}", database);
        }
    }

    /// <summary>把目标库重置回 MULTI_USER（恢复中断时可能停留在 RESTORING/SINGLE_USER）。失败仅告警。</summary>
    internal async Task TrySetMultiUserAsync(string database, CancellationToken ct)
    {
        try
        {
            // 恢复中断时目标库可能处于 RESTORING/SINGLE_USER——须从 master 连接重置
            await using var connection = new SqlConnection(_masterConnectionString());
            await connection.OpenAsync(ct);
            await using var command = new SqlCommand($"ALTER DATABASE [{database}] SET MULTI_USER", connection)
            {
                CommandTimeout = 60
            };
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BACKUP] 恢复中断后重置多用户模式失败：{Database}", database);
        }
    }
}
