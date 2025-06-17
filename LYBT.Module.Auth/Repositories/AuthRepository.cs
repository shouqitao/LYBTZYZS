using System;
using System.Threading.Tasks;
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
    }
}
