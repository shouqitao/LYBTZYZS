using LYBT.Desktop.Contracts.Enums;

namespace LYBT.Desktop.Contracts.Models;

/// <summary>
/// 本地模式数据库配置（B-07 初始化向导 Step 2）。
/// </summary>
/// <remarks>
/// <para>默认值 = 嵌入式 LocalWebAPI 的历史硬编码目标（LocalDB <c>LYBTDesktop</c>），
/// 未配置过时行为与既有安装完全一致。</para>
/// <para><see cref="Password"/> 仅存在于内存；落盘时经 DPAPI 加密（见
/// <c>ILocalDatabaseSettingsService</c>），不写入明文 JSON。</para>
/// </remarks>
public sealed record LocalDatabaseProfile
{
    /// <summary>LocalDB 默认实例地址</summary>
    public const string DefaultLocalDbServer = @"(localdb)\MSSQLLocalDB";

    /// <summary>默认数据库名（与既有嵌入式 LocalWebAPI 目标一致）</summary>
    public const string DefaultDatabase = "LYBTDesktop";

    /// <summary>提供程序（LocalDB / SQL Server）</summary>
    public LocalDatabaseProvider Provider { get; init; } = LocalDatabaseProvider.LocalDb;

    /// <summary>实例地址（LocalDB 为 <c>(localdb)\MSSQLLocalDB</c>；SQL Server 为 <c>主机\实例</c> 或 <c>主机,端口</c>）</summary>
    public string Server { get; init; } = DefaultLocalDbServer;

    /// <summary>数据库名</summary>
    public string Database { get; init; } = DefaultDatabase;

    /// <summary>是否使用 Windows 集成认证（false = SQL Server 账号密码）</summary>
    public bool UseWindowsAuthentication { get; init; } = true;

    /// <summary>SQL Server 登录名（<see cref="UseWindowsAuthentication"/> 为 false 时必填）</summary>
    public string? UserId { get; init; }

    /// <summary>SQL Server 登录密码（仅内存；落盘经 DPAPI）</summary>
    public string? Password { get; init; }

    /// <summary>默认配置（LocalDB + 默认库名，与既有安装一致）</summary>
    public static LocalDatabaseProfile Default => new();

    /// <summary>构造连接字符串（与嵌入式宿主消费方唯一约定）</summary>
    public string BuildConnectionString()
    {
        var parts = new List<string>
        {
            $"Server={Server.Trim()}",
            $"Database={Database.Trim()}"
        };

        if (UseWindowsAuthentication)
        {
            parts.Add("Trusted_Connection=True");
        }
        else
        {
            parts.Add($"User ID={UserId?.Trim()}");
            parts.Add($"Password={Password}");
        }

        parts.Add("MultipleActiveResultSets=true");
        parts.Add("TrustServerCertificate=True");

        // 仅 SQL Server 显式启用传输加密（LocalDB 保持历史连接串语义不变）
        if (Provider == LocalDatabaseProvider.SqlServer)
            parts.Add("Encrypt=True");

        return string.Join(";", parts);
    }
}
