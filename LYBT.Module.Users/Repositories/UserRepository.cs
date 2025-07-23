using LYBT.Infrastructure;
using LYBT.Module.Users.Dtos;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Users.Repositories {

    /// <summary>
    /// 用户仓储实现类（基于EF Core）
    /// </summary>
    public class UserRepository : IUserRepository {
        private readonly AppDbContext _dbContext;

        public UserRepository(AppDbContext dbContext) {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        public async Task<bool> AddAsync(UserModel user) {
            await _dbContext.Users.AddAsync(user);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        public async Task<bool> UpdateAsync(UserModel user) {
            _dbContext.Users.Update(user);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<bool> DisableAsync(Guid id) {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
                return false;

            user.IsActive = false;
            _dbContext.Users.Update(user);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<bool> EnableAsync(Guid id) {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
                return false;

            user.IsActive = true;
            _dbContext.Users.Update(user);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 分页条件查找用户
        /// </summary>
        public async Task<(IList<UserModel> users, int total)> GetPagedAsync(UserQueryDto query) {
            var dbSet = _dbContext.Users.AsQueryable();

            // 隐藏内置的sysadmin用户
            dbSet = dbSet.Where(u => u.UserName != "sysadmin");

            // 关键词（用户名、真实姓名、拼音码）模糊查找
            if (!string.IsNullOrWhiteSpace(query.Keyword)) {
                var keyword = query.Keyword.ToUpperInvariant();
                dbSet = dbSet.Where(u =>
                    u.UserName.Contains(query.Keyword) ||
                    u.RealName.Contains(query.Keyword) ||
                    u.PinyinCode.Contains(keyword));
            }

            // 角色筛选
            if (query.Roles != null && query.Roles.Count > 0) {
                dbSet = dbSet.Where(u => query.Roles.Any(r => u.Roles.Contains(r)));
            }

            // 启用状态筛选
            if (query.IsActive.HasValue) {
                dbSet = dbSet.Where(u => u.IsActive == query.IsActive.Value);
            }

            // 获取总数
            int total = await dbSet.CountAsync();

            // 分页查询
            int skip = (query.Page - 1) * query.PageSize;
            var users = await dbSet
                .OrderByDescending(u => u.CreatedTime)
                .Skip(skip)
                .Take(query.PageSize)
                .ToListAsync();

            return (users, total);
        }

        /// <summary>
        /// 根据用户名查找
        /// </summary>
        public async Task<UserModel?> GetByUsernameAsync(string userName) {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserName == userName);
        }

        /// <summary>
        /// 根据ID查找
        /// </summary>
        public async Task<UserModel?> GetByIdAsync(Guid id) {
            return await _dbContext.Users.FindAsync(id);
        }

        /// <summary>
        /// 根据ID列表批量获取用户
        /// </summary>
        public async Task<List<UserModel>> GetUsersByIdsAsync(List<Guid> ids) {
            return await _dbContext.Users
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();
        }

        /// <summary>
        /// 校验用户名是否存在
        /// </summary>
        public async Task<bool> ExistsByUsernameAsync(string userName) {
            return await _dbContext.Users.AnyAsync(u => u.UserName == userName);
        }

        /// <summary>
        /// 更新用户密码
        /// </summary>
        public async Task<bool> UpdatePasswordAsync(Guid id, string passwordHash) {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
                return false;

            user.PasswordHash = passwordHash;
            _dbContext.Users.Update(user);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 批量更新启用状态
        /// </summary>
        public async Task<int> UpdateActiveStatusAsync(List<Guid> ids, bool isActive) {
            var affectedRows = await _dbContext.Users
                .Where(u => ids.Contains(u.Id))
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.IsActive, isActive));

            return affectedRows;
        }
    }
}