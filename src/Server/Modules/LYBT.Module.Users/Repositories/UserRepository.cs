using System.Linq.Expressions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Repositories
{
    /// <summary>
    /// 用户仓储实现 - 实现IRepository<User>标准接口
    /// Phase 1 Task 1.2: 基础数据模块Repository层统一重构
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 统一共性：实现IRepository<User>的11个标准CRUD方法
    /// - 保持特性：保留用户模块特定业务方法
    /// - 软删除模式：所有查询自动过滤IsDeleted=true的数据
    /// - 查询优化：只读查询使用AsNoTracking提升性能
    /// </remarks>
    internal class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<User> _dbSet;
        private readonly ILogger<UserRepository>? _logger;

        public UserRepository(AppDbContext context, ILogger<UserRepository>? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<User>();
            _logger = logger;
        }

        #region IRepository<User> 标准方法实现

        /// <summary>
        /// 根据ID获取用户（包含软删除过滤）
        /// </summary>
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        /// <summary>
        /// 获取所有用户（⚠️ 仅用于下拉列表等小数据量场景）
        /// </summary>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        /// <summary>
        /// 分页查询用户（支持用户名/真实姓名/拼音码搜索）
        /// </summary>
        public async Task<PagedResult<User>> GetPagedAsync(int pageNumber, int pageSize, string? keyword = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted);

            // 关键字搜索：用户名、真实姓名、拼音码
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchTerm = keyword.Trim();
                query = query.Where(u =>
                    u.UserName.Contains(searchTerm) ||
                    (u.RealName != null && u.RealName.Contains(searchTerm)) ||
                    (u.PinYinCode != null && u.PinYinCode.Contains(searchTerm))
                );
            }

            query = query.OrderBy(u => u.UserName);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<User>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// 条件查询（⚠️ 谨慎使用，建议使用具体业务方法）
        /// </summary>
        public async Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .Where(predicate)
                .ToListAsync();
        }

        /// <summary>
        /// 获取单个用户（条件查询）
        /// </summary>
        public async Task<User?> GetSingleAsync(Expression<Func<User, bool>> predicate)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        public async Task<User> AddAsync(User entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        public async Task<User> UpdateAsync(User entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 检查用户是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.AnyAsync(u => u.Id == id && !u.IsDeleted);
        }

        /// <summary>
        /// 获取用户总数
        /// </summary>
        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync(u => !u.IsDeleted);
        }

        /// <summary>
        /// 保存更改（⚠️ 通常由Service层调用）
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion

        #region IUserRepository 特定业务方法

        /// <summary>
        /// 根据用户名获取用户（支持用户名或邮箱登录）
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(u => 
                    (u.UserName == username || u.Email == username) && 
                    !u.IsDeleted);
        }

        /// <summary>
        /// 检查用户名是否已存在
        /// </summary>
        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return await _dbSet
                .AnyAsync(u => u.UserName == username && !u.IsDeleted);
        }

        #endregion
    }
}
