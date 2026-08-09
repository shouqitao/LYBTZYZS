using System.Collections.ObjectModel;
using System.Data;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace LYBT.Shared.Logging.Sinks;

/// <summary>
/// MSSqlServer sink 配置选项（代码优先，替代 appsettings.json 中的 MSSqlServer WriteTo 配置）
/// </summary>
/// <remarks>
/// 背景：Serilog.Sinks.MSSqlServer 不支持混合 JSON 配置 sink + 代码配置列选项，
/// 因此 MSSqlServer sink 完全通过代码配置（A-31-C1 收敛自 WebAPI/Extensions/SerilogMSSqlServerExtensions）。
/// appsettings.Production.json 中的 MSSqlServer WriteTo 条目已移除，避免 autoCreateSqlTable 配置冲突——代码优先。
/// </remarks>
public sealed class MssqlSinkOptions
{
    /// <summary>
    /// 日志表名
    /// </summary>
    public string TableName { get; set; } = "SystemLogs";

    /// <summary>
    /// 表所属 Schema
    /// </summary>
    public string SchemaName { get; set; } = "dbo";

    /// <summary>
    /// 是否自动建表
    /// 代码优先：默认 false，由 EF Core 迁移管理表结构，避免冲突
    /// </summary>
    public bool AutoCreateSqlTable { get; set; }

    /// <summary>
    /// 批次写入条数上限
    /// </summary>
    public int BatchPostingLimit { get; set; } = 50;

    /// <summary>
    /// 批次写入周期
    /// </summary>
    public TimeSpan BatchPeriod { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 最低日志级别
    /// </summary>
    public LogEventLevel RestrictedToMinimumLevel { get; set; } = LogEventLevel.Warning;
}

/// <summary>
/// Serilog MSSqlServer sink 配置扩展（A-31-C1 收敛自 WebAPI/Extensions/SerilogMSSqlServerExtensions）
/// 以编程方式配置列选项，替代 appsettings.json 中的 columnOptionsSection
/// </summary>
public static class MssqlSinkConfiguration
{
    /// <summary>
    /// 向 LoggerConfiguration 添加 MSSqlServer sink，包含完整的列选项配置
    /// </summary>
    /// <param name="loggerConfiguration">LoggerConfiguration 实例</param>
    /// <param name="connectionString">数据库连接字符串（从 DatabaseOptions.ConnectionString 读取）</param>
    /// <param name="configure">sink 选项配置</param>
    /// <returns>配置后的 LoggerConfiguration</returns>
    public static LoggerConfiguration WriteToMssqlSink(
        this LoggerConfiguration loggerConfiguration,
        string? connectionString,
        Action<MssqlSinkOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return loggerConfiguration;

        var sinkOptions = new MssqlSinkOptions();
        configure?.Invoke(sinkOptions);

        var mssqlOptions = new MSSqlServerSinkOptions
        {
            TableName = sinkOptions.TableName,
            SchemaName = sinkOptions.SchemaName,
            AutoCreateSqlTable = sinkOptions.AutoCreateSqlTable,
            BatchPostingLimit = sinkOptions.BatchPostingLimit,
            BatchPeriod = sinkOptions.BatchPeriod
        };

        return loggerConfiguration.WriteTo.MSSqlServer(
            connectionString: connectionString,
            sinkOptions: mssqlOptions,
            columnOptions: BuildColumnOptions(),
            restrictedToMinimumLevel: sinkOptions.RestrictedToMinimumLevel);
    }

    /// <summary>
    /// 构建 MSSqlServer 列选项（对应原 columnOptionsSection JSON 配置）
    /// </summary>
    private static ColumnOptions BuildColumnOptions()
    {
        var options = new ColumnOptions();

        // disableTriggers: true
        options.DisableTriggers = true;

        // clusteredColumnstoreIndex: false
        options.ClusteredColumnstoreIndex = false;

        // addStandardColumns: ["LogEvent"]
        options.Store.Add(StandardColumn.LogEvent);

        // removeStandardColumns: ["MessageTemplate", "Level", "TimeStamp", "Exception", "Properties"]
        options.Store.Remove(StandardColumn.MessageTemplate);
        options.Store.Remove(StandardColumn.Level);
        options.Store.Remove(StandardColumn.TimeStamp);
        options.Store.Remove(StandardColumn.Exception);
        options.Store.Remove(StandardColumn.Properties);

        // primaryKeyColumnName: "Id"
        // id: { columnName: "Id", nonClusteredIndex: false }
        options.PrimaryKey = options.Id;
        options.Id.ColumnName = "Id";
        options.Id.NonClusteredIndex = false;

        // message: { columnName: "Message" }
        options.Message.ColumnName = "Message";

        // exception: { columnName: "Exception" }
        options.Exception.ColumnName = "Exception";

        // messageTemplate: { columnName: "LoggerName" }
        options.MessageTemplate.ColumnName = "LoggerName";

        // properties: { columnName: "Properties" }
        options.Properties.ColumnName = "Properties";

        // level: { columnName: "Level", storeAsEnum: false }
        options.Level.ColumnName = "Level";
        options.Level.StoreAsEnum = false;

        // timeStamp: { columnName: "Timestamp" }
        options.TimeStamp.ColumnName = "Timestamp";
        options.TimeStamp.ConvertToUtc = false;

        // additionalColumns
        options.AdditionalColumns = new Collection<SqlColumn>
        {
            new SqlColumn { ColumnName = "UserId",        DataType = SqlDbType.UniqueIdentifier, AllowNull = true },
            new SqlColumn { ColumnName = "RequestId",     DataType = SqlDbType.NVarChar,         DataLength = 36,  AllowNull = true },
            new SqlColumn { ColumnName = "CorrelationId", DataType = SqlDbType.NVarChar,         DataLength = 36,  AllowNull = true },
            new SqlColumn { ColumnName = "MachineName",   DataType = SqlDbType.NVarChar,         DataLength = 100, AllowNull = true },
            new SqlColumn { ColumnName = "ThreadId",      DataType = SqlDbType.Int,              AllowNull = true },
        };

        return options;
    }
}
