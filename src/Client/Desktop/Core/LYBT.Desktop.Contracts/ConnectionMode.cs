namespace LYBT.Desktop.Contracts;

/// <summary>
/// 连接模式枚举 (SYNC-D02/D03)
/// 定义客户端与数据源的连接方式。
/// </summary>
public enum ConnectionMode
{
    /// <summary>
    /// 远程模式 - 通过远程 WebAPI 服务器连接 (云/服务器)
    /// </summary>
    Remote,

    /// <summary>
    /// 本地模式 - 通过本地 WebAPI 连接 (localhost, 独立进程)
    /// </summary>
    Local
}
