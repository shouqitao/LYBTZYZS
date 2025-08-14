using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using SharedUserPagedQueryDto = LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto;

namespace LYBT.Module.Users.Repositories
{

    /// <summary>
    /// 用户仓储实现类 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，实现用户特定业务逻辑
    /// 实现软删除策略：用户只能禁用/启用，不能物理删除
    /// </summary>
    public class UserRepository : BaseRepository<UserModel>, LYBT.Module.Users.Interfaces.IUserRepository
    {
        public UserRepository(AppDbContext dbContext) : base(dbContext)
        {
            // BaseRepository会处理基础的数据库操作
        }

        // 注意：AddAsync, UpdateAsync等基础CRUD方法由BaseRepository提供

        /// <summary>
        /// 禁用用户（软删除）
        /// </summary>
        public async Task<bool> DisableAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            user.Status = CommonStatus.Disabled;
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<bool> EnableAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            user.Status = CommonStatus.Enabled;
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 分页条件查找用户
        /// 权限控制：禁用的用户仅管理员可查询
        /// </summary>
        public async Task<(IList<UserModel> users, int total)> GetPagedAsync(SharedUserPagedQueryDto query, bool includeDisabled = false)
        {
            var dbSet = _context.Users.AsQueryable();

            // 隐藏内置的sysadmin用户
            dbSet = dbSet.Where(u => u.Username != "sysadmin");

            // 权限控制：非管理员只能看到启用的用户
            if (!includeDisabled)
            {
                dbSet = dbSet.Where(u => u.Status == CommonStatus.Enabled);
            }

            // 通用搜索关键词（模糊搜索：用户名、真实姓名、拼音码）
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                dbSet = dbSet.Where(u =>
                    u.Username.Contains(keyword) ||
                    u.RealName.Contains(keyword) ||
                    (u.PinYinCode != null && u.PinYinCode.Contains(keyword.ToUpperInvariant()))
                );
            }
            // 特定字段搜索（精确搜索）
            else
            {
                if (!string.IsNullOrWhiteSpace(query.Username))
                {
                    dbSet = dbSet.Where(u => u.Username.Contains(query.Username));
                }
                if (!string.IsNullOrWhiteSpace(query.RealName))
                {
                    dbSet = dbSet.Where(u => u.RealName.Contains(query.RealName));
                }
                if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                {
                    dbSet = dbSet.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(query.PhoneNumber));
                }
                if (!string.IsNullOrWhiteSpace(query.PinYinCode))
                {
                    var keyword = query.PinYinCode.ToUpperInvariant();
                    dbSet = dbSet.Where(u => u.PinYinCode != null && u.PinYinCode.Contains(keyword));
                }
            }

            // 角色筛选（已移除Role字段）
            // 角色功能已合并到用户模块中

            // 状态筛选
            if (query.Status.HasValue)
            {
                dbSet = dbSet.Where(u => u.Status == query.Status.Value);
            }

            // 获取总数
            int total = await dbSet.CountAsync();

            // 日期范围筛选
            if (query.StartDate.HasValue)
            {
                dbSet = dbSet.Where(u => u.CreateTime >= query.StartDate.Value);
            }
            if (query.EndDate.HasValue)
            {
                dbSet = dbSet.Where(u => u.CreateTime <= query.EndDate.Value);
            }
            if (query.LoginStartDate.HasValue)
            {
                dbSet = dbSet.Where(u => u.LastLoginTime >= query.LoginStartDate.Value);
            }
            if (query.LoginEndDate.HasValue)
            {
                dbSet = dbSet.Where(u => u.LastLoginTime <= query.LoginEndDate.Value);
            }

            // 分页查询
            int skip = (query.PageIndex - 1) * query.PageSize;
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
        public async Task<UserModel?> GetByUsernameAsync(string userName)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == userName);
        }

        /// <summary>
        /// 根据ID查找
        /// 权限控制：禁用的用户仅管理员可查询
        /// </summary>
        public async Task<UserModel?> GetByIdAsync(Guid id, bool includeDisabled = false)
        {
            var query = _context.Users.AsQueryable();

            if (!includeDisabled)
            {
                query = query.Where(u => u.Status == CommonStatus.Enabled);
            }

            return await query.FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// 根据ID列表批量获取用户
        /// 权限控制：禁用的用户仅管理员可查询
        /// </summary>
        public async Task<List<UserModel>> GetUsersByIdsAsync(List<Guid> ids, bool includeDisabled = false)
        {
            if (!ids.Any())
            {
                return new List<UserModel>();
            }

            // 使用原生SQL避免EF Core的Contains转换问题
            var idStrings = string.Join("','", ids.Select(id => id.ToString()));
            var sql = includeDisabled
                ? $"SELECT * FROM Users WHERE Id IN ('{idStrings}')"
                : $"SELECT * FROM Users WHERE Id IN ('{idStrings}') AND Status = 0";

            return await _context.Users.FromSqlRaw(sql).ToListAsync();
        }

        /// <summary>
        /// 校验用户名是否存在（包括禁用用户）
        /// </summary>
        public async Task<bool> ExistsByUsernameAsync(string userName)
        {
            return await _context.Users.AnyAsync(u => u.Username == userName);
        }

        /// <summary>
        /// 更新用户密码
        /// </summary>
        public async Task<bool> UpdatePasswordAsync(Guid id, string passwordHash)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            user.PasswordHash = passwordHash;
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 批量更新启用状态
        /// </summary>
        public async Task<int> UpdateActiveStatusAsync(List<Guid> ids, bool isActive)
        {
            if (!ids.Any())
            {
                return 0;
            }

            // 使用原生SQL避免EF Core的Contains转换问题
            var idStrings = string.Join("','", ids.Select(id => id.ToString()));
            var sql = $"UPDATE Users SET Status = {(isActive ? 0 : 1)} WHERE Id IN ('{idStrings}')";

            return await _context.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        public async Task<List<UserModel>> GetActiveUsersAsync()
        {
            return await _context.Users
                .Where(u => u.Status == CommonStatus.Enabled && u.Username != "sysadmin")
                .OrderBy(u => u.RealName)
                .ToListAsync();
        }


        // 注意：GetAllAsync方法由BaseRepository提供
    }
}