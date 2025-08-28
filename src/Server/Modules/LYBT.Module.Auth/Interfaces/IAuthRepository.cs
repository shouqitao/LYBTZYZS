using LYBT.Entities.Users;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 登录验证仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展认证特定业务方法
    /// </summary>
    public interface IAuthRepository : IBaseRepository<User>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义认证特有的业务方法

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<User?> GetByUsernameAsync(string userName);

        /// <summary>
        /// 更新最后登录时间
        /// </summary>
        Task UpdateLastLoginTimeAsync(Guid id, DateTime loginTime);

        /// <summary>
        /// 获取管理员密码哈希
        /// </summary>
        Task<string?> GetAdminPasswordHashAsync(string userName);

        /// <summary>
        /// 更新管理员密码哈希
        /// </summary>
        Task UpdateAdminPasswordHashAsync(string userName, string passwordHash);

        /// <summary>
        /// 更新登录防护相关字段（失败次数、锁定时间）
        /// </summary>
        Task UpdateUserLoginProtectionAsync(User user);

        /// <summary>
        /// 更新用户安全状态 - UltraThink Phase 3 安全增强
        /// </summary>
        /// <param name="userId">用户ID</param>        /// <param name="failedLoginCount">失败登录次数</param>        /// <param name="lockoutEnd">锁定结束时间</param>
        Task UpdateUserSecurityAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd);
    }
}
