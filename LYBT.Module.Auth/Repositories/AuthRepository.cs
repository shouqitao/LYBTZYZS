using LYBT.Infrastructure;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Repositories {

    /// <summary>
    /// 登录验证仓储实现
    /// </summary>
    public class AuthRepository : IAuthRepository {
        private readonly AppDbContext _dbContext;

        public AuthRepository(AppDbContext dbContext) {
            _dbContext = dbContext;
        }

        public async Task<UserModel?> GetByUsernameAsync(string userName) {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task UpdateLastLoginTimeAsync(Guid id, DateTime loginTime) {
            var user = await _dbContext.Users.FindAsync(id);
            if (user != null) {
                user.LastLoginTime = loginTime;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<string?> GetAdminPasswordHashAsync(string userName) {
            var secret = await _dbContext.AdminSecrets.FirstOrDefaultAsync(s => s.UserName == userName);
            return secret?.PasswordHash;
        }

        public async Task UpdateAdminPasswordHashAsync(string userName, string passwordHash) {
            var secret = await _dbContext.AdminSecrets.FirstOrDefaultAsync(s => s.UserName == userName);
            if (secret != null) {
                secret.PasswordHash = passwordHash;
                _dbContext.AdminSecrets.Update(secret);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task UpdateUserLoginProtectionAsync(UserModel user) {
            var dbUser = await _dbContext.Users.FindAsync(user.Id);
            if (dbUser != null) {
                dbUser.FailedLoginCount = user.FailedLoginCount;
                dbUser.LockoutEnd = user.LockoutEnd;
                _dbContext.Users.Update(dbUser);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}