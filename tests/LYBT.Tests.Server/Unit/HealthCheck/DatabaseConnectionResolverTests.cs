using FluentAssertions;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.WebAPI.HealthCheck;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 数据库连接串 fallback 链单测（HEALTHCHECK-FALLBACK-FIX 回归：
/// HealthCheck 与数据库注册必须同源——Database 节 → ConnectionStrings:DefaultConnection
/// → CONNECTION_STRING 环境变量——防止「实际运行连接串判为未配置」复发）
/// </summary>
public class DatabaseConnectionResolverTests
{
    [Fact]
    public void Resolve_DatabaseSection_FirstPriority()
    {
        var config = BuildConfig("cs:default", "cs:db-section");
        var options = new DatabaseOptions { ConnectionString = "cs:db-section" };

        DatabaseConnectionResolver.Resolve(config, options).Should().Be("cs:db-section");
    }

    [Fact]
    public void Resolve_ConnectionStrings_WhenDatabaseSectionEmpty()
    {
        var config = BuildConfig("cs:default", null);
        var options = new DatabaseOptions { ConnectionString = null };

        DatabaseConnectionResolver.Resolve(config, options).Should().Be("cs:default");
    }

    [Fact]
    public void Resolve_ConnectionStrings_WithEnvVarOverride()
    {
        // ConnectionStrings__DefaultConnection 环境变量覆盖（生产注入模式）——须先 Set 再 Build（配置链读取时环境变量已存在）
        var original = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "cs:env");
            var config = BuildConfig("cs:file", null);
            var options = new DatabaseOptions { ConnectionString = null };
            DatabaseConnectionResolver.Resolve(config, options).Should().Be("cs:env");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", original);
        }
    }

    [Fact]
    public void Resolve_ConnectionStringEnvVar_LastPriority()
    {
        var original = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        try
        {
            Environment.SetEnvironmentVariable("CONNECTION_STRING", "cs:legacy-env");
            var config = BuildConfig(null, null);
            var options = new DatabaseOptions { ConnectionString = null };
            DatabaseConnectionResolver.Resolve(config, options).Should().Be("cs:legacy-env");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CONNECTION_STRING", original);
        }
    }

    [Fact]
    public void Resolve_AllEmpty_ReturnsEmpty()
    {
        var config = BuildConfig(null, null);
        var options = new DatabaseOptions { ConnectionString = null };

        DatabaseConnectionResolver.Resolve(config, options).Should().BeEmpty();
    }

    private static IConfiguration BuildConfig(string? connectionString, string? dbSection)
    {
        var builder = new ConfigurationBuilder();
        if (connectionString is not null)
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            });
        if (dbSection is not null)
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = dbSection
            });
        return builder
            .AddEnvironmentVariables() // 对齐生产配置链（环境变量最后 = 最高优先——ConnectionStrings__DefaultConnection 覆盖）
            .Build();
    }
}
