using System.Threading.Tasks;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 安全凭据存储服务接口 - Issue #1246
    /// 使用 Windows DPAPI 加密存储用户名和密码
    /// </summary>
    public interface ISecureCredentialStorage
    {
        /// <summary>
        /// 保存凭据（用户名 + 密码）
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码（将被 DPAPI 加密）</param>
        /// <param name="rememberPassword">是否记住密码</param>
        Task SaveCredentialsAsync(string username, string password, bool rememberPassword);

        /// <summary>
        /// 加载已保存的凭据
        /// </summary>
        /// <returns>凭据元组（用户名, 密码），如果不存在或解密失败则返回 null</returns>
        Task<(string Username, string Password)?> LoadCredentialsAsync();

        /// <summary>
        /// 检查是否启用了"记住密码"
        /// </summary>
        Task<bool> IsRememberPasswordEnabledAsync();

        /// <summary>
        /// 清除已保存的凭据
        /// </summary>
        Task ClearCredentialsAsync();
    }
}
