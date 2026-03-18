namespace LYBT.Desktop.Contracts.Initialization;

/// <summary>
/// 数据库初始化器接口
/// 负责确保本地数据库已初始化（创建表结构、种子数据等）
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// 确保数据库已初始化（线程安全，幂等）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
}
