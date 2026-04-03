using System.Collections.ObjectModel;
using System.Data;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// Serilog MSSqlServer sink 扩展方法
/// 以编程方式配置列选项，替代 appsettings.json 中的 columnOptionsSection
/// </summary>
/// <remarks>
/// 背景：Serilog.Sinks.MSSqlServer 不支持混合 JSON 配置 sink + 代码配置列选项，
/// 因此 MSSqlServer sink 完全通过代码配置，连接字符串和 sink 选项从配置文件读取。
///
/// appsettings.json 中的 Serilog.WriteTo[MSSqlServer].Args.sinkOptionsSection 保持不变，
/// 作为表名/Schema/批次大小等参数的来源说明文档。
/// 实际参数值在本方法中硬编码，与 JSON 值保持一致。
/// </remarks>
public static class SerilogMSSqlServerExtensions
{
    /// <summary>
    /// 向 LoggerConfiguration 添加 MSSqlServer sink，包含完整的列选项配置
    /// </summary>
    /// <param name="loggerConfiguration">LoggerConfiguration 实例</param>
    /// <param name="connectionString">数据库连接字符串（从 ConnectionStrings:DefaultConnection 读取）</param>
    /// <returns>配置后的 LoggerConfiguration</returns>
    public static LoggerConfiguration AddMSSqlServerSinkWithColumnOptions(
        this LoggerConfiguration loggerConfiguration,
        string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return loggerConfiguration;

        var columnOptions = BuildColumnOptions();

        var sinkOptions = new MSSqlServerSinkOptions
        {
            // 与 appsettings.json Serilog.WriteTo[MSSqlServer].Args.sinkOptionsSection 保持一致
            TableName = "SystemLogs",
            SchemaName = "dbo",
            AutoCreateSqlTable = true,
            BatchPostingLimit = 50,
            BatchPeriod = TimeSpan.FromSeconds(5)
        };

        return loggerConfiguration.WriteTo.MSSqlServer(
            connectionString: connectionString,
            sinkOptions: sinkOptions,
            columnOptions: columnOptions,
            restrictedToMinimumLevel: LogEventLevel.Warning);
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
