using LYBT.Module.Users.Models;

namespace LYBT.Module.Auth.Interfaces {

    /// <summary>
    /// 登录验证仓储接口
    /// </summary>
    public interface IAuthRepository {

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<UserModel?> GetByUsernameAsync(string userName);

        /// <summary>
        /// 更新最后登录时间
        /// </summary>
        Task UpdateLastLoginTimeAsync(Guid id, DateTime loginTime);

        /// <summary>
        /// 获取管理员密码哈希
        /// </summary>
        Task<string?> GetAdminPasswordHashAsync(string userName);

        /// <summary>
        /// 更新登录防护相关字段（失败次数、锁定时间）
        /// </summary>
        Task UpdateUserLoginProtectionAsync(UserModel user);
    }
}