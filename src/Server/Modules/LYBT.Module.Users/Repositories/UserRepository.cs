using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Repositories
{
    /// <summary>
    /// 用户仓储实现 - 继承BaseRepository并实现IUserRepository
    /// Task 1.4: Repository重构，适配新的简化Repository设计
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 继承BaseRepository：复用11个标准CRUD方法
    /// - 业务扩展：实现用户特定的业务查询方法
    /// - 软删除模式：所有查询自动过滤IsDeleted=true的数据
    /// - 查询优化：只读查询使用AsNoTracking提升性能
    /// </remarks>
    internal class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
            : base(context, logger)
        {
        }

        #region 模板方法覆盖 - 用户关键字搜索和排序

        /// <summary>
        /// 关键字过滤：用户名、真实姓名、拼音码
        /// </summary>
        protected override IQueryable<User> ApplyKeywordFilter(IQueryable<User> query, string keyword)
        {
            return query.Where(u =>
                u.UserName.Contains(keyword) ||
                (u.RealName != null && u.RealName.Contains(keyword)) ||
                (u.PinYinCode != null && u.PinYinCode.Contains(keyword))
            );
        }

        /// <summary>
        /// 默认排序：按用户名升序
        /// </summary>
        protected override IQueryable<User> ApplyDefaultOrdering(IQueryable<User> query)
        {
            return query.OrderBy(u => u.UserName);
        }

        #endregion

        #region Sprint3-X6: 分页查询（DB 层 keyword/role/status 筛选）

        /// <summary>
        /// 分页查询用户（支持 keyword/role/status 筛选，DB 层执行）
        /// Sprint3-X6: 从 Service 内存过滤迁移到 Repository DB 查询
        /// </summary>
        public async Task<PagedResult<User>> GetPagedAsync(
            int pageNumber, int pageSize, string? keyword, UserRole? role, CommonStatus? status, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Where(e => !e.IsDeleted);

            // 关键字过滤（复用模板方法）
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = ApplyKeywordFilter(query, keyword.Trim());
            }

            // 角色筛选（DB 层执行）
            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            // 状态筛选（DB 层执行）
            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status.Value);
            }

            // 默认排序（复用模板方法）
            query = ApplyDefaultOrdering(query);

            return await GetPagedResultAsync(query, pageNumber, pageSize, cancellationToken);
        }

        #endregion

        #region IUserRepository 特定业务方法

        /// <summary>
        /// 根据用户名获取用户（支持用户名或邮箱登录）
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            var user = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    (u.UserName == username || u.Email == username) &&
                    !u.IsDeleted, cancellationToken);

            // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
            _logger.LogDebug("[REPO] User.GetByUsername({Username}) → {Result}",
                username, user != null ? "Found" : "NotFound");

            return user;
        }

        /// <summary>
        /// 检查用户名是否已存在
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return await _dbSet
                .AnyAsync(u => u.UserName == username && !u.IsDeleted, cancellationToken);
        }

        #endregion

        #region OpenSpec: optimize-module-list-ui - 恢复功能支持

        /// <summary>
        /// 根据ID获取实体（包括已软删除的）
        /// 使用IgnoreQueryFilters绕过全局软删除过滤器
        /// 注: FindAsync在EF Core 8中会应用全局查询过滤器，无法查到已删除记录
        /// </summary>
        public async Task<User?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        #endregion
    }
}