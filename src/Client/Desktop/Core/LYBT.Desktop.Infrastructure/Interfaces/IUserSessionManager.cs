using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 用户会话管理接口 - UltraThink架构会话管理扩展
    /// 继承基础会话管理，提供更高级的会话功能
    /// </summary>
    public interface IUserSessionManager : ISessionManager
    {
        /// <summary>
        /// 保存用户会话到本地存储
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="token">认证Token</param>
        /// <returns>保存任务</returns>
        Task SaveSessionAsync(UserDto user, string token);

        /// <summary>
        /// 从本地存储恢复用户会话
        /// </summary>
        /// <returns>恢复任务</returns>
        Task<bool> RestoreSessionAsync();

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="user">新的用户信息</param>
        void UpdateUserInfo(UserDto user);

        /// <summary>
        /// 延长会话时间
        /// </summary>
        /// <returns>延长任务</returns>
        Task ExtendSessionAsync();

        /// <summary>
        /// 获取会话剩余时间
        /// </summary>
        /// <returns>剩余时间</returns>
        TimeSpan? GetSessionRemainingTime();

        /// <summary>
        /// 检查会话是否需要刷新
        /// </summary>
        /// <returns>是否需要刷新</returns>
        bool ShouldRefreshSession();

        /// <summary>
        /// 获取最后活动时间
        /// </summary>
        /// <returns>最后活动时间</returns>
        DateTime? GetLastActivityTime();

        /// <summary>
        /// 更新最后活动时间
        /// </summary>
        void UpdateLastActivityTime();

        /// <summary>
        /// 用户信息更新事件
        /// </summary>
        event EventHandler<UserDto>? UserInfoUpdated;
    }
}
