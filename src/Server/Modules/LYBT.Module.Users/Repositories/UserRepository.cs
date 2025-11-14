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

        /// <summary>
        /// 初始化用户仓储实例
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="logger">日志记录器</param>
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
        /// 批量新增用户
        /// Phase 6: IRepository批量操作方法实现（Epic #2016）
        /// </summary>
        public async Task<IEnumerable<User>> AddRangeAsync(IEnumerable<User> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            foreach (var entity in entityList)
            {
                entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            }

            await _dbSet.AddRangeAsync(entityList);
            await SaveChangesAsync();

            return entityList;
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
        /// 批量软删除用户（根据实体集合）
        /// Phase 6: IRepository批量操作方法实现（Epic #2016）
        /// </summary>
        public async Task<int> DeleteRangeAsync(IEnumerable<User> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            var deletedCount = 0;

            foreach (var entity in entityList)
            {
                if (!entity.IsDeleted)
                {
                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.Now;
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
            {
                _dbSet.UpdateRange(entityList.Where(e => e.IsDeleted));
                await SaveChangesAsync();
            }

            return deletedCount;
        }

        /// <summary>
        /// 批量软删除用户（根据ID集合）
        /// Phase 6: IRepository批量操作方法实现（Epic #2016）
        /// </summary>
        public async Task<int> DeleteRangeAsync(IEnumerable<Guid> ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (!idList.Any())
                return 0;

            var entities = await _dbSet
                .Where(e => !e.IsDeleted && idList.Contains(e.Id))
                .ToListAsync();

            if (!entities.Any())
                return 0;

            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.Now;
            }

            _dbSet.UpdateRange(entities);
            await SaveChangesAsync();

            return entities.Count;
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
