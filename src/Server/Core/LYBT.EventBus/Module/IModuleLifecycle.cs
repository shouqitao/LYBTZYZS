namespace LYBT.EventBus.Module;

/// <summary>
/// 模块生命周期管理接口
/// 提供模块启动、停止、重启等生命周期管理功能
/// </summary>
public interface IModuleLifecycle
{
    /// <summary>
    /// 初始化模块
    /// 在模块首次加载时调用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化任务</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动模块
    /// 在模块需要激活时调用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动任务</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止模块
    /// 在模块需要停用时调用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理模块资源
    /// 在模块被卸载时调用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理任务</returns>
    Task DisposeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 健康检查
    /// 检查模块是否处于健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    Task<ModuleHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 重启模块
    /// 先停止再启动模块
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重启任务</returns>
    async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }
}
