using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Configuration;

namespace LYBT.Shared.Configuration;

/// <summary>
/// 数据库连接字符串解析
/// 三级回退：DatabaseOptions.ConnectionString → ConnectionStrings:DefaultConnection → CONNECTION_STRING 环境变量
/// 空串/空白视为未配置，继续回退；全部缺失返回空字符串
/// </summary>
public static class ConnectionStringResolver
{
    public static string GetEffectiveConnectionString(DatabaseOptions? databaseOptions, IConfiguration configuration)
    {
        var connectionString = FirstNonEmpty(
            databaseOptions?.ConnectionString,
            configuration.GetConnectionString("DefaultConnection"),
            Environment.GetEnvironmentVariable("CONNECTION_STRING"));

        return connectionString ?? string.Empty;
    }

    private static string? FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;
        }

        return null;
    }
}
