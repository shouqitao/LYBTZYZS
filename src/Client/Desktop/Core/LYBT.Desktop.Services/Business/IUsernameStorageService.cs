namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 用户名存储服务接口 - Issue #861
    /// 专门管理"记住用户名"功能，与 Token 存储分离
    /// </summary>
    public interface IUsernameStorageService
    {
        /// <summary>
        /// 保存用户名
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="rememberMe">是否记住用户名</param>
        Task SaveUsernameAsync(string username, bool rememberMe);

        /// <summary>
        /// 获取已保存的用户名
        /// </summary>
        /// <returns>用户名，如果未保存则返回 null</returns>
        Task<string?> GetSavedUsernameAsync();

        /// <summary>
        /// 检查是否启用了"记住用户名"
        /// </summary>
        /// <returns>如果启用返回 true，否则返回 false</returns>
        Task<bool> IsRememberMeEnabledAsync();

        /// <summary>
        /// 清除已保存的用户名
        /// </summary>
        Task ClearUsernameAsync();
    }
}
