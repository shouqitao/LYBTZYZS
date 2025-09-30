namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 用户体验优化服务接口
    /// UltraThink用户体验增强
    /// </summary>
    public interface IUserExperienceService
    {
        /// <summary>
        /// 初始化用户体验优化
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 优化界面响应性
        /// </summary>
        void OptimizeUIResponsiveness();

        /// <summary>
        /// 设置用户偏好
        /// </summary>
        /// <param name="preferenceName">偏好名称</param>
        /// <param name="value">偏好值</param>
        void SetUserPreference(string preferenceName, object value);

        /// <summary>
        /// 获取用户偏好
        /// </summary>
        /// <typeparam name="T">偏好值类型</typeparam>
        /// <param name="preferenceName">偏好名称</param>
        /// <returns>偏好值</returns>
        T GetUserPreference<T>(string preferenceName);

        /// <summary>
        /// 记录用户操作
        /// </summary>
        /// <param name="action">操作名称</param>
        void LogUserAction(string action);
    }
}
