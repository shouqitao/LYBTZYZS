namespace LYBT.Desktop.Foundation.Application;

/// <summary>
/// 连接模式枚举
/// OpenSpec: refactor-startup-connection-resilience - 预留本地模式入口
/// </summary>
public enum ConnectionMode
{
    /// <summary>
    /// 远程模式 - 通过WebAPI服务器连接
    /// </summary>
    Remote,

    /// <summary>
    /// 本地模式 - 直连本地数据库（待独立提案实现）
    /// </summary>
    Local
}
