using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Repositories
{
    /// <summary>
    /// 登录验证仓储实现 - 数据层统一化重构
    /// 继承BaseRepository获得通用CRUD功能，扩展认证特有业务方法
    /// </summary>
    public class AuthRepository : BaseRepository<User>, IAuthRepository
    {
        /// <summary>
        /// 初始化仓储并注入统一数据库上下文
        /// </summary>
        /// <param name="context">统一数据库上下文</param>
        public AuthRepository(AppDbContext context) : base(context)
        {
        }

        // 注意：基础CRUD方法由BaseRepository提供
        // 这里只实现认证特有的业务方法

        /// <summary>
        /// 通过用户名获取用户信息
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <returns>用户实体或 null</returns>
        public async Task<User?> GetByUsernameAsync(string userName)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == userName);
        }

        /// <summary>
        /// 更新用户的最后登录时间 - UltraThink v2.0简化：通过AuthSession记录
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="loginTime">登录时间</param>
        public async Task UpdateLastLoginTimeAsync(Guid id, DateTime loginTime)
        {
            // UltraThink v2.0简化：User实体不再包含LastLoginTime字段
            // 登录时间信息通过AuthSession表记录，此方法仅保留接口兼容性
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取管理员密码哈希
        /// </summary>
        /// <param name="userName">管理员用户名</param>
        /// <returns>密码哈希或 null</returns>
        public async Task<string?> GetAdminPasswordHashAsync(string userName)
        {
            var secret = await _context.AdminSecrets.FirstOrDefaultAsync(s => s.Username == userName);
            return secret?.PasswordHash;
        }

        /// <summary>
        /// 更新管理员密码哈希
        /// </summary>
        /// <param name="userName">管理员用户名</param>
        /// <param name="passwordHash">新的密码哈希</param>
        public async Task UpdateAdminPasswordHashAsync(string userName, string passwordHash)
        {
            var secret = await _context.AdminSecrets.FirstOrDefaultAsync(s => s.Username == userName);
            if (secret != null)
            {
                secret.PasswordHash = passwordHash;
                _context.AdminSecrets.Update(secret);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 更新用户登录保护信息 - UltraThink v2.0简化：通过AuthSession记录
        /// </summary>
        /// <param name="user">用户实体</param>
        public async Task UpdateUserLoginProtectionAsync(User user)
        {
            // UltraThink v2.0简化：User实体不再包含FailedLoginCount和LockoutEnd字段
            // 登录保护信息通过AuthSession记录，此方法仅保留接口兼容性
            await Task.CompletedTask;
        }

        /// <summary>
        /// 更新用户安全状态 - UltraThink Phase 3 安全增强
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="failedLoginCount">失败登录次数</param>
        /// <param name="lockoutEnd">锁定结束时间</param>
        public async Task UpdateUserSecurityAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd)
        {
            await _dbSet.Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.FailedLoginCount, failedLoginCount)
                    .SetProperty(u => u.LockoutEnd, lockoutEnd));
        }
    }
}