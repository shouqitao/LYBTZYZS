using LYBT.Desktop.Auth.Models;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 连接设置服务接口 - Issue #1825
/// 负责管理远程/本地连接模式的持久化配置
/// </summary>
public interface IConnectionSettingsService
{
    /// <summary>
    /// 获取当前连接模式
    /// </summary>
    ConnectionMode GetConnectionMode();

    /// <summary>
    /// 保存连接模式
    /// </summary>
    void SaveConnectionMode(ConnectionMode mode);

    /// <summary>
    /// 是否记住上次选择
    /// </summary>
    bool RememberLastChoice { get; set; }
}
