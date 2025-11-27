namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 用户活动状态查询接口
/// OpenSpec: refactor-token-sliding-expiration (AUTH-002)
/// 供Foundation层查询用户活跃状态，避免循环依赖
/// </summary>
public interface IUserActivityState
{
    /// <summary>
    /// 用户是否活跃（在配置的超时时间内有活动）
    /// </summary>
    bool IsUserActive { get; }

    /// <summary>
    /// 重置活动计时器（Token刷新成功后调用）
    /// </summary>
    void ResetActivity();
}
