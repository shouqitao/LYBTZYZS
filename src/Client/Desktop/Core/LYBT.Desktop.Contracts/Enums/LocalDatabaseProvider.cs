namespace LYBT.Desktop.Contracts.Enums;

/// <summary>
/// 本地库提供程序（B-07 初始化向导 Step 2：本地模式数据库连接配置）。
/// </summary>
public enum LocalDatabaseProvider
{
    /// <summary>SQL Server LocalDB（<c>(localdb)\MSSQLLocalDB</c>，默认——无需安装实例）</summary>
    LocalDb = 0,

    /// <summary>SQL Server 实例（本机或局域网，需提供实例地址与认证方式）</summary>
    SqlServer = 1
}
