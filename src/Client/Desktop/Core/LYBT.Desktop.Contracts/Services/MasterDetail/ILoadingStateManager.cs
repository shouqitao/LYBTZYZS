namespace LYBT.Desktop.Contracts.Services.MasterDetail;

/// <summary>
/// 加载状态管理接口
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 管理ViewModel的忙碌状态和加载消息
/// </summary>
public interface ILoadingStateManager
{
    /// <summary>
    /// 是否处于忙碌状态
    /// </summary>
    bool IsBusy { get; set; }

    /// <summary>
    /// 忙碌状态消息
    /// </summary>
    string? BusyMessage { get; set; }

    /// <summary>
    /// 设置忙碌状态
    /// </summary>
    /// <param name="isBusy">是否忙碌</param>
    /// <param name="message">状态消息</param>
    void SetBusy(bool isBusy, string? message = null);

    /// <summary>
    /// 创建自动释放的忙碌状态作用域
    /// </summary>
    /// <param name="message">状态消息</param>
    /// <returns>可释放的作用域对象</returns>
    IDisposable BeginBusy(string? message = null);
}
