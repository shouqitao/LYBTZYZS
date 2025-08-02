using LYBT.Infrastructure.Data;
using LYBT.Models.Users;
using LYBT.Module.Users.Interfaces;
using SharedUserPagedQueryDto = LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Users.Repositories {

    /// <summary>
    /// 用户仓储实现类（基于EF Core统一数据库上下文）
    /// 实现软删除策略：用户只能禁用/启用，不能物理删除
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
        /// 禁用用户（软删除）
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
        /// 权限控制：禁用的用户仅管理员可查询
        /// </summary>
        public async Task<(IList<UserModel> users, int total)> GetPagedAsync(SharedUserPagedQueryDto query, bool includeDisabled = false) {
            var dbSet = _dbContext.Users.AsQueryable();

            // 隐藏内置的sysadmin用户
            dbSet = dbSet.Where(u => u.Username != "sysadmin");

            // 权限控制：非管理员只能看到启用的用户
            if (!includeDisabled) {
                dbSet = dbSet.Where(u => u.IsActive);
            }

            // 关键词查找：支持用户名、真实姓名、邮箱、电话、部门、职位、拼音码等多维度搜索
            if (!string.IsNullOrWhiteSpace(query.Username)) {
                dbSet = dbSet.Where(u => u.Username.Contains(query.Username));
            }
            if (!string.IsNullOrWhiteSpace(query.RealName)) {
                dbSet = dbSet.Where(u => u.RealName.Contains(query.RealName));
            }
            if (!string.IsNullOrWhiteSpace(query.Email)) {
                dbSet = dbSet.Where(u => u.Email != null && u.Email.Contains(query.Email));
            }
            if (!string.IsNullOrWhiteSpace(query.PhoneNumber)) {
                dbSet = dbSet.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(query.PhoneNumber));
            }
            // 注释掉Department和Position查询，因为这些字段在数据库中不存在
            // if (!string.IsNullOrWhiteSpace(query.Department)) {
            //     dbSet = dbSet.Where(u => u.Department.Contains(query.Department));
            // }
            // if (!string.IsNullOrWhiteSpace(query.Position)) {
            //     dbSet = dbSet.Where(u => u.Position.Contains(query.Position));
            // }
            if (!string.IsNullOrWhiteSpace(query.PinyinCode)) {
                var keyword = query.PinyinCode.ToUpperInvariant();
                dbSet = dbSet.Where(u => u.PinyinCode != null && u.PinyinCode.Contains(keyword));
            }

            // 角色筛选
            if (query.Role.HasValue) {
                dbSet = dbSet.Where(u => u.Role == query.Role.Value);
            }

            // 启用状态筛选
            if (query.IsActive.HasValue) {
                dbSet = dbSet.Where(u => u.IsActive == query.IsActive.Value);
            }

            // 获取总数
            int total = await dbSet.CountAsync();

            // 日期范围筛选
            if (query.CreateStartDate.HasValue) {
                dbSet = dbSet.Where(u => u.CreateTime >= query.CreateStartDate.Value);
            }
            if (query.CreateEndDate.HasValue) {
                dbSet = dbSet.Where(u => u.CreateTime <= query.CreateEndDate.Value);
            }
            if (query.LoginStartDate.HasValue) {
                dbSet = dbSet.Where(u => u.LastLoginTime >= query.LoginStartDate.Value);
            }
            if (query.LoginEndDate.HasValue) {
                dbSet = dbSet.Where(u => u.LastLoginTime <= query.LoginEndDate.Value);
            }

            // 分页查询
            int skip = (query.CurrentPage - 1) * query.PageSize;
            var users = await dbSet
                .OrderByDescending(u => u.CreateTime)
                .Skip(skip)
                .Take(query.PageSize)
                .ToListAsync();

            return (users, total);
        }

        /// <summary>
        /// 根据用户名查找（包括禁用用户，用于登录验证）
        /// </summary>
        public async Task<UserModel?> GetByUsernameAsync(string userName) {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == userName);
        }

        /// <summary>
        /// 根据ID查找
        /// 权限控制：禁用的用户仅管理员可查询
        /// </summary>
        public async Task<UserModel?> GetByIdAsync(Guid id, bool includeDisabled = false) {
            var query = _dbContext.Users.AsQueryable();

            if (!includeDisabled) {
                query = query.Where(u => u.IsActive);
            }

            return await query.FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// 根据ID列表批量获取用户
        /// 权限控制：禁用的用户仅管理员可查询
        /// </summary>
        public async Task<List<UserModel>> GetUsersByIdsAsync(List<Guid> ids, bool includeDisabled = false) {
            if (!ids.Any()) {
                return new List<UserModel>();
            }

            // 使用原生SQL避免EF Core的Contains转换问题
            var idStrings = string.Join("','", ids.Select(id => id.ToString()));
            var sql = includeDisabled 
                ? $"SELECT * FROM Users WHERE Id IN ('{idStrings}')"
                : $"SELECT * FROM Users WHERE Id IN ('{idStrings}') AND IsActive = 1";

            return await _dbContext.Users.FromSqlRaw(sql).ToListAsync();
        }

        /// <summary>
        /// 校验用户名是否存在（包括禁用用户）
        /// </summary>
        public async Task<bool> ExistsByUsernameAsync(string userName) {
            return await _dbContext.Users.AnyAsync(u => u.Username == userName);
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
            if (!ids.Any()) {
                return 0;
            }

            // 使用原生SQL避免EF Core的Contains转换问题
            var idStrings = string.Join("','", ids.Select(id => id.ToString()));
            var sql = $"UPDATE Users SET IsActive = {(isActive ? 1 : 0)} WHERE Id IN ('{idStrings}')";
            
            return await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        public async Task<List<UserModel>> GetActiveUsersAsync() {
            return await _dbContext.Users
                .Where(u => u.IsActive && u.Username != "sysadmin")
                .OrderBy(u => u.RealName)
                .ToListAsync();
        }
    }
}