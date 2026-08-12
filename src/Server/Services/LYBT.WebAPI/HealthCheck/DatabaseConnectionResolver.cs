using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Configuration;

namespace LYBT.WebAPI.HealthCheck;

/// <summary>
/// 数据库连接字符串解析（HEALTHCHECK-FALLBACK-FIX: 统一 fallback 链——
/// DatabaseServiceCollectionExtensions 注册处与 SqlServerHealthCheck 共用，
/// 防两处复制漂移——本次 bug 正是 HealthCheck 只读 Database:ConnectionString
/// 导致实际运行连接串（ConnectionStrings:DefaultConnection 环境变量注入）判为未配置）
/// </summary>
public static class DatabaseConnectionResolver
{
    /// <summary>
    /// 解析连接字符串（对齐 DatabaseServiceCollectionExtensions 注册链）：
    /// 1) DatabaseOptions.ConnectionString（Database 节）
    /// 2) ConnectionStrings:DefaultConnection（GetConnectionString——含 ConnectionStrings__DefaultConnection 环境变量覆盖）
    /// 3) CONNECTION_STRING 环境变量
    /// </summary>
    public static string Resolve(IConfiguration configuration, DatabaseOptions options)
    {
        var fromOptions = options.ConnectionString;
        if (!string.IsNullOrWhiteSpace(fromOptions))
            return fromOptions;

        var fromConnectionStrings = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromConnectionStrings))
            return fromConnectionStrings;

        return Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? string.Empty;
    }
}
