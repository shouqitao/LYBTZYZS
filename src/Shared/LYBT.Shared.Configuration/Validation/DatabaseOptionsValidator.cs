using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Options;

namespace LYBT.Shared.Configuration.Validation;

/// <summary>
/// 数据库配置验证器
/// </summary>
public sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var failures = new List<string>();

        // 验证连接池配置
        if (options.ConnectionPool.MinConnections > options.ConnectionPool.MaxConnections)
        {
            failures.Add("MinConnections 不能大于 MaxConnections");
        }

        // 验证重试策略配置
        if (options.RetryPolicy.BaseDelayMs > options.RetryPolicy.MaxDelayMs)
        {
            failures.Add("BaseDelayMs 不能大于 MaxDelayMs");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
