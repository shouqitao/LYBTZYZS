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
    /// 用户仓储实现（简化版本，使用统一数据库上下文）
    /// 遵循适度设计原则，避免过度的模块化设计
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<User> _dbSet;
        private readonly ILogger<UserRepository>? _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">统一数据库上下文</param>
        /// <param name="logger">日志记录器</param>
        public UserRepository(AppDbContext context, ILogger<UserRepository>? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<User>();
            _logger = logger;
        }

        #region IRepository<User> 实现

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public async Task<PagedResult<User>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _dbSet.CountAsync();
            var items = await _dbSet
                .AsNoTracking()
                .OrderBy(u => u.Email)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<User>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PagedResult<User>> GetPagedAsync(
            Expression<Func<User, bool>>? predicate,
            int pageNumber,
            int pageSize,
            Expression<Func<User, object>>? orderBy = null,
            bool ascending = true)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync();

            if (orderBy != null)
            {
                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            }
            else
            {
                query = query.OrderBy(u => u.Email);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<User>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<User?> GetSingleAsync(Expression<Func<User, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.AnyAsync(u => u.Id == id);
        }

        public async Task<bool> ExistsAsync(Expression<Func<User, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<long> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public async Task<long> CountAsync(Expression<Func<User, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        public async Task<User> AddAsync(User entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<User>> AddRangeAsync(IEnumerable<User> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            var userList = entities.ToList();
            await _dbSet.AddRangeAsync(userList);
            await _context.SaveChangesAsync();
            return userList;
        }

        public async Task<User> UpdateAsync(User entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(User entity)
        {
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return false;

            return await DeleteAsync(entity);
        }

        public async Task<int> DeleteRangeAsync(IEnumerable<User> entities)
        {
            if (entities == null) return 0;

            var count = 0;
            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                count++;
            }

            await _context.SaveChangesAsync();
            return count;
        }

        public async Task<int> DeleteRangeAsync(IEnumerable<Guid> ids)
        {
            var entities = await _dbSet.Where(u => ids.Contains(u.Id)).ToListAsync();
            return await DeleteRangeAsync(entities);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion

        #region IUserRepository 特定方法

        public async Task<User?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            // 支持用户名或邮箱登录
            return await _dbSet
                .FirstOrDefaultAsync(u => (u.UserName == username || u.Email == username) && !u.IsDeleted);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return await _dbSet
                .AnyAsync(u => u.Email == username && !u.IsDeleted);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return await _dbSet
                .AnyAsync(u => u.Email == email && !u.IsDeleted);
        }

        #endregion
    }
}
