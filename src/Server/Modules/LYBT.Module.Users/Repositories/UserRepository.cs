using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
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
