namespace LYBT.Infrastructure.Configuration.Stores;

/// <summary>
/// 运行时配置覆盖存储接口
/// 独立于 AppDbContext（满足架构约束 P10），负责配置覆盖的持久化
/// </summary>
public interface IConfigurationStore
{
    /// <summary>
    /// 加载全部覆盖配置项
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> LoadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置单个覆盖配置项（与 appsettings 默认值相同时自动移除覆盖）
    /// </summary>
    Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除单个覆盖配置项（恢复默认值）
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
