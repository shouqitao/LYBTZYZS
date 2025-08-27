using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories.Base;
using LYBT.Entities;
using LYBT.Entities.Users;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Infrastructure.Repositories
{
    /// <summary>
    /// 用户Repository实现 - UltraThink重构架构
    /// 基于DDD Repository模式的用户数据访问层
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByUserNameAsync(string userName);
        Task<User> GetByEmailAsync(string email);
        Task<List<User>> GetDoctorsAsync();
        Task<bool> ExistsAsync(string userName, string email = null);
        Task<PagedResult<User>> GetPagedAsync(UserPagedQueryDto query);
        Task<List<User>> SearchAsync(string keyword);
        Task<UserStatisticsDto> GetStatisticsAsync(DateTime? startDate, DateTime? endDate);
        Task<bool> UpdatePasswordAsync(Guid userId, string passwordHash);
        Task<bool> UpdateLastLoginAsync(Guid userId, DateTime loginTime);
        Task<int> GetActiveCountAsync();
    }

    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        public UserRepository(AppDbContext context, ILogger<UserRepository> logger) 
            : base(context, logger)
        {
        }

        #region Specific Query Methods

        public async Task<User> GetByUserNameAsync(string userName)
        {
            Logger.LogDebug("Getting user by username: {UserName}", userName);
            
            return await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == userName);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            Logger.LogDebug("Getting user by email: {Email}", email);
            
            return await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<User>> GetDoctorsAsync()
        {
            Logger.LogDebug("Getting all doctors");
            
            return await DbSet
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                .OrderBy(u => u.RealName)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string userName, string email = null)
        {
            Logger.LogDebug("Checking user existence: {UserName}, {Email}", userName, email);
            
            var query = DbSet.AsNoTracking();
            
            if (!string.IsNullOrEmpty(email))
            {
                return await query.AnyAsync(u => u.Username == userName || u.Email == email);
            }
            
            return await query.AnyAsync(u => u.Username == userName);
        }

        public async Task<PagedResult<User>> GetPagedAsync(UserPagedQueryDto query)
        {
            Logger.LogDebug("Getting paged users: Page {Page}, Size {Size}, Search: {Search}", 
                query.PageIndex, query.PageSize, query.Keyword);

            var queryable = DbSet.AsNoTracking();

            // 角色过滤
            if (!string.IsNullOrEmpty(query.Role))
            {
                if (Enum.TryParse<UserRole>(query.Role, out var userRole))
                {
                    queryable = queryable.Where(u => u.Role == userRole);
                }
            }

            // 活跃状态过滤
            if (query.Status.HasValue)
            {
                queryable = queryable.Where(u => u.Status == query.Status.Value);
            }

            // 搜索过滤
            if (!string.IsNullOrEmpty(query.Keyword))
            {
                var searchTerm = query.Keyword.ToLower();
                queryable = queryable.Where(u => 
                    u.Username.ToLower().Contains(searchTerm) ||
                    u.RealName.ToLower().Contains(searchTerm) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
            }

            // UltraThink v2.0简化：移除日期范围过滤（时间字段已删除）
            // 如需时间过滤，可通过审计日志实现

            // 总数统计
            var totalCount = await queryable.CountAsync();

            // 排序 - UltraThink v2.0简化：使用Username作为默认排序
            queryable = query.SortField?.ToLower() switch
            {
                "username" => query.IsDescending 
                    ? queryable.OrderByDescending(u => u.Username)
                    : queryable.OrderBy(u => u.Username),
                    
                "realname" => query.IsDescending 
                    ? queryable.OrderByDescending(u => u.RealName)
                    : queryable.OrderBy(u => u.RealName),
                    
                "email" => query.IsDescending 
                    ? queryable.OrderByDescending(u => u.Email)
                    : queryable.OrderBy(u => u.Email),
                    
                "role" => query.IsDescending 
                    ? queryable.OrderByDescending(u => u.Role)
                    : queryable.OrderBy(u => u.Role),
                    
                _ => queryable.OrderBy(u => u.Username) // 默认按用户名排序
            };

            // 分页
            var items = await queryable
                .Skip(query.PageIndex * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<User>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        public async Task<List<User>> SearchAsync(string keyword)
        {
            Logger.LogDebug("Searching users with keyword: {Keyword}", keyword);
            
            if (string.IsNullOrEmpty(keyword))
            {
                return new List<User>();
            }

            var searchTerm = keyword.ToLower();
            
            return await DbSet
                .AsNoTracking()
                .Where(u => u.Status == CommonStatus.Enabled && (
                    u.Username.ToLower().Contains(searchTerm) ||
                    u.RealName.ToLower().Contains(searchTerm) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm))))
                .OrderBy(u => u.RealName)
                .Take(20) // 限制搜索结果数量
                .ToListAsync();
        }

        public async Task<UserStatisticsDto> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            Logger.LogDebug("Getting user statistics from {StartDate} to {EndDate}", startDate, endDate);

            var query = DbSet.AsNoTracking();
            
            // UltraThink v2.0简化：移除日期范围过滤（时间字段已删除）
            // 统计直接基于当前所有用户

            var stats = await query.GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalCount = await query.CountAsync();
            var activeCount = await query.CountAsync(u => u.Status == CommonStatus.Enabled);

            return new UserStatisticsDto
            {
                TotalCount = totalCount,
                ActiveCount = activeCount,
                InactiveCount = totalCount - activeCount,
                DoctorCount = stats.FirstOrDefault(s => s.Role == UserRole.Doctor)?.Count ?? 0,
                AdminCount = stats.FirstOrDefault(s => s.Role == UserRole.Admin)?.Count ?? 0,
                PharmacistCount = stats.FirstOrDefault(s => s.Role == UserRole.Pharmacist)?.Count ?? 0,
                ReceptionistCount = stats.FirstOrDefault(s => s.Role == UserRole.Receptionist)?.Count ?? 0,
                CashierCount = stats.FirstOrDefault(s => s.Role == UserRole.Cashier)?.Count ?? 0,
                TherapistCount = stats.FirstOrDefault(s => s.Role == UserRole.Therapist)?.Count ?? 0
            };
        }

        #endregion

        #region Update Operations

        public async Task<bool> UpdatePasswordAsync(Guid userId, string passwordHash)
        {
            Logger.LogDebug("Updating password for user: {UserId}", userId);
            
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.PasswordHash = passwordHash;
            // UltraThink v2.0简化：移除UpdateTime字段
            
            Context.Entry(user).Property(u => u.PasswordHash).IsModified = true;
            
            return await SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateLastLoginAsync(Guid userId, DateTime loginTime)
        {
            Logger.LogDebug("Last login tracking - UltraThink v2.0简化：登录时间通过AuthSession记录，User实体不再存储");
            
            // UltraThink v2.0简化：User实体不再包含LastLoginTime字段
            // 登录时间信息通过AuthSession表记录
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // 仅记录日志，不修改User实体
            Logger.LogInformation("User {UserId} logged in at {LoginTime}", userId, loginTime);
            return true;
        }

        public async Task<int> GetActiveCountAsync()
        {
            Logger.LogDebug("Getting active user count");
            
            return await DbSet
                .AsNoTracking()
                .CountAsync(u => u.Status == CommonStatus.Enabled);
        }

        #endregion

        #region Protected Override Methods

        protected override IQueryable<User> ApplySearch(IQueryable<User> queryable, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return queryable;
            }

            var lowerSearchTerm = searchTerm.ToLower();
            
            return queryable.Where(u => 
                u.Username.ToLower().Contains(lowerSearchTerm) ||
                u.RealName.ToLower().Contains(lowerSearchTerm) ||
                (u.Email != null && u.Email.ToLower().Contains(lowerSearchTerm)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(lowerSearchTerm)));
        }

        protected override IQueryable<User> ApplyDefaultSorting(IQueryable<User> queryable)
        {
            // UltraThink v2.0简化：使用Username作为默认排序（User实体已删除CreateTime字段）
            return queryable.OrderBy(u => u.Username)
                           .ThenBy(u => u.RealName);
        }

        #endregion
    }
}