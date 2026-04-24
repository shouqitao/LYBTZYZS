namespace LYBT.Desktop.Contracts;

/// <summary>
/// 连接模式枚举 (SYNC-D02/D03)
/// 定义客户端与数据源的连接方式。
/// </summary>
public enum ConnectionMode
{
    /// <summary>
    /// 远程模式 - 通过 WebAPI 服务器连接
    /// </summary>
    Remote,

    /// <summary>
    /// 本地模式 - 直连本地数据库 (SQL Server LocalDB)
    /// </summary>
    Local
    , // NEW: Embedded Kestrel → SQLite
    LocalWebAPI
}
