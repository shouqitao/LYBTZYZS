using LYBT.Infrastructure.Data;
using LYBT.Models.Users;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Repositories
{

    /// <summary>
    /// 登录验证仓储实现（使用统一数据库上下文）
    /// </summary>
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// 初始化仓储并注入统一数据库上下文
        /// </summary>
        /// <param name="dbContext">统一数据库上下文</param>
        public AuthRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 通过用户名获取用户信息
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <returns>用户实体或 null</returns>
        public async Task<UserModel?> GetByUsernameAsync(string userName)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == userName);
        }

        /// <summary>
        /// 更新用户的最后登录时间
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="loginTime">登录时间</param>
        public async Task UpdateLastLoginTimeAsync(Guid id, DateTime loginTime)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user != null)
            {
                user.LastLoginTime = loginTime;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 获取管理员密码哈希
        /// </summary>
        /// <param name="userName">管理员用户名</param>
        /// <returns>密码哈希或 null</returns>
        public async Task<string?> GetAdminPasswordHashAsync(string userName)
        {
            var secret = await _dbContext.AdminSecrets.FirstOrDefaultAsync(s => s.Username == userName);
            return secret?.PasswordHash;
        }

        /// <summary>
        /// 更新管理员密码哈希
        /// </summary>
        /// <param name="userName">管理员用户名</param>
        /// <param name="passwordHash">新的密码哈希</param>
        public async Task UpdateAdminPasswordHashAsync(string userName, string passwordHash)
        {
            var secret = await _dbContext.AdminSecrets.FirstOrDefaultAsync(s => s.Username == userName);
            if (secret != null)
            {
                secret.PasswordHash = passwordHash;
                _dbContext.AdminSecrets.Update(secret);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 更新用户登录保护信息，如失败次数和锁定时间
        /// </summary>
        /// <param name="user">包含最新登录保护信息的用户实体</param>
        public async Task UpdateUserLoginProtectionAsync(UserModel user)
        {
            var dbUser = await _dbContext.Users.FindAsync(user.Id);
            if (dbUser != null)
            {
                dbUser.FailedLoginCount = user.FailedLoginCount;
                dbUser.LockoutEnd = user.LockoutEnd;
                _dbContext.Users.Update(dbUser);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}