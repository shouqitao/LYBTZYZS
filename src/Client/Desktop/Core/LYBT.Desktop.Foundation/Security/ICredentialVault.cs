namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 凭据保险库接口 - 安全存储密码和AutoLoginToken
    /// OpenSpec: refactor-login-authentication (CVT-001, CVT-002)
    /// OpenSpec: redesign-login-remember-password - 添加密码存储功能
    ///
    /// 设计原则：
    /// 1. 使用DPAPI加密 + HMAC完整性校验
    /// 2. 只有当前Windows用户能访问
    /// 3. 支持密码存储（用户选择"记住密码"时）
    /// 4. 支持AutoLoginToken存储（用户选择"自动登录"时）
    ///
    /// 存储位置: %LOCALAPPDATA%\LYBT\Desktop\vault.dat
    /// </summary>
    public interface ICredentialVault
    {
        #region 密码存储 (OpenSpec: redesign-login-remember-password)

        /// <summary>
        /// 保存密码（DPAPI加密）
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">明文密码</param>
        /// <returns>保存是否成功</returns>
        Task<bool> SavePasswordAsync(string username, string password);

        /// <summary>
        /// 获取已保存的密码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>明文密码，未找到返回null</returns>
        Task<string?> GetPasswordAsync(string username);

        /// <summary>
        /// 检查是否存在已保存的密码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>true=存在已保存密码</returns>
        Task<bool> HasSavedPasswordAsync(string username);

        /// <summary>
        /// 清除已保存的密码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>清除是否成功</returns>
        Task<bool> ClearPasswordAsync(string username);

        #endregion

        #region AutoLoginToken存储

        /// <summary>
        /// 保存AutoLoginToken
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="autoLoginToken">服务器生成的自动登录令牌</param>
        /// <returns>保存是否成功</returns>
        Task<bool> SaveAutoLoginTokenAsync(string username, string autoLoginToken);

        /// <summary>
        /// 获取已保存的AutoLoginToken
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>AutoLoginToken，未找到返回null</returns>
        Task<string?> GetAutoLoginTokenAsync(string username);

        /// <summary>
        /// 清除指定用户的凭据
        /// </summary>
        /// <param name="username">用户名，传null清除所有</param>
        /// <returns>清除是否成功</returns>
        Task<bool> ClearCredentialsAsync(string? username = null);

        /// <summary>
        /// 验证存储数据的完整性（HMAC校验）
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>true=完整，false=被篡改或损坏</returns>
        Task<bool> VerifyIntegrityAsync(string username);

        /// <summary>
        /// 迁移旧格式凭据（一次性操作）
        /// 将旧的credentials.dat迁移到新的vault.dat格式
        /// </summary>
        Task MigrateOldFormatAsync();

        /// <summary>
        /// 检查是否存在有效的AutoLoginToken
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>true=存在有效令牌</returns>
        Task<bool> HasValidTokenAsync(string username);

        #endregion
    }
}
