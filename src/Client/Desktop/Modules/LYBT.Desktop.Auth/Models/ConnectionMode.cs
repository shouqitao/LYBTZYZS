namespace LYBT.Desktop.Auth.Models;

/// <summary>
/// 连接模式枚举 - Issue #1825
/// 用于切换远程API模式和本地数据库模式
/// </summary>
public enum ConnectionMode
{
    /// <summary>
    /// 远程模式 - 连接到远程WebAPI服务
    /// </summary>
    Remote = 0,

    /// <summary>
    /// 本地模式 - 使用本地数据库（v2.0实现）
    /// </summary>
    Local = 1
}
