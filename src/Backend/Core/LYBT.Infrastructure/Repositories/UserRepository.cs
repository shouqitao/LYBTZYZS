using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories.Base;
using LYBT.Models;
using LYBT.Models.Users;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Repositories
{
    /// <summary>
    /// 用户Repository实现 - UltraThink重构架构
    /// 基于DDD Repository模式的用户数据访问层
    /// </summary>
    public interface IUserRepository : IRepository<UserModel>
    {
        Task<UserModel> GetByUserNameAsync(string userName);
        Task<UserModel> GetByEmailAsync(string email);
        Task<List<UserModel>> GetDoctorsAsync();
        Task<bool> ExistsAsync(string userName, string email = null);
        Task<PagedResult<UserModel>> GetPagedAsync(UserPagedQueryDto query);
        Task<List<UserModel>> SearchAsync(string keyword);
        Task<UserStatisticsDto> GetStatisticsAsync(DateTime? startDate, DateTime? endDate);
        Task<bool> UpdatePasswordAsync(Guid userId, string passwordHash);
        Task<bool> UpdateLastLoginAsync(Guid userId, DateTime loginTime);
        Task<int> GetActiveCountAsync();
    }

    public class UserRepository : RepositoryBase<UserModel>, IUserRepository
    {
        public UserRepository(AppDbContext context, ILogger<UserRepository> logger) 
            : base(context, logger)
        {
        }

        #region Specific Query Methods

        public async Task<UserModel> GetByUserNameAsync(string userName)
        {
            Logger.LogDebug("Getting user by username: {UserName}", userName);
            
            return await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<UserModel> GetByEmailAsync(string email)
        {
            Logger.LogDebug("Getting user by email: {Email}", email);
            
            return await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<UserModel>> GetDoctorsAsync()
        {
            Logger.LogDebug("Getting all doctors");
            
            return await DbSet
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Doctor && u.IsActive)
                .OrderBy(u => u.RealName)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string userName, string email = null)
        {
            Logger.LogDebug("Checking user existence: {UserName}, {Email}", userName, email);
            
            var query = DbSet.AsNoTracking();
            
            if (!string.IsNullOrEmpty(email))
            {
                return await query.AnyAsync(u => u.UserName == userName || u.Email == email);
            }
            
            return await query.AnyAsync(u => u.UserName == userName);
        }

        public async Task<PagedResult<UserModel>> GetPagedAsync(UserPagedQueryDto query)
        {
            Logger.LogDebug("Getting paged users: Page {Page}, Size {Size}, Search: {Search}", 
                query.PageIndex, query.PageSize, query.SearchTerm);

            var queryable = DbSet.AsNoTracking();

            // 角色过滤
            if (query.Role.HasValue)
            {
                queryable = queryable.Where(u => u.Role == query.Role.Value);
            }

            // 活跃状态过滤
            if (query.IsActive.HasValue)
            {
                queryable = queryable.Where(u => u.IsActive == query.IsActive.Value);
            }

            // 搜索过滤
            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.ToLower();
                queryable = queryable.Where(u => 
                    u.UserName.ToLower().Contains(searchTerm) ||
                    u.RealName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
            }

            // 日期范围过滤
            if (query.StartDate.HasValue)
            {
                queryable = queryable.Where(u => u.CreatedAt >= query.StartDate.Value);
            }
            
            if (query.EndDate.HasValue)
            {
                queryable = queryable.Where(u => u.CreatedAt <= query.EndDate.Value);
            }

            // 总数统计
            var totalCount = await queryable.CountAsync();

            // 排序
            queryable = query.SortField?.ToLower() switch
            {
                "username" => query.SortDirection?.ToLower() == "desc" 
                    ? queryable.OrderByDescending(u => u.UserName)
                    : queryable.OrderBy(u => u.UserName),
                    
                "realname" => query.SortDirection?.ToLower() == "desc" 
                    ? queryable.OrderByDescending(u => u.RealName)
                    : queryable.OrderBy(u => u.RealName),
                    
                "email" => query.SortDirection?.ToLower() == "desc" 
                    ? queryable.OrderByDescending(u => u.Email)
                    : queryable.OrderBy(u => u.Email),
                    
                "createdat" => query.SortDirection?.ToLower() == "desc" 
                    ? queryable.OrderByDescending(u => u.CreatedAt)
                    : queryable.OrderBy(u => u.CreatedAt),
                    
                _ => queryable.OrderByDescending(u => u.CreatedAt) // 默认排序
            };

            // 分页
            var items = await queryable
                .Skip(query.PageIndex * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<UserModel>(items, totalCount, query.PageIndex, query.PageSize);
        }

        public async Task<List<UserModel>> SearchAsync(string keyword)
        {
            Logger.LogDebug("Searching users with keyword: {Keyword}", keyword);
            
            if (string.IsNullOrEmpty(keyword))
            {
                return new List<UserModel>();
            }

            var searchTerm = keyword.ToLower();
            
            return await DbSet
                .AsNoTracking()
                .Where(u => u.IsActive && (
                    u.UserName.ToLower().Contains(searchTerm) ||
                    u.RealName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm)))
                .OrderBy(u => u.RealName)
                .Take(20) // 限制搜索结果数量
                .ToListAsync();
        }

        public async Task<UserStatisticsDto> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            Logger.LogDebug("Getting user statistics from {StartDate} to {EndDate}", startDate, endDate);

            var query = DbSet.AsNoTracking();
            
            if (startDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= endDate.Value);
            }

            var stats = await query.GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalCount = await query.CountAsync();
            var activeCount = await query.CountAsync(u => u.IsActive);

            return new UserStatisticsDto
            {
                TotalCount = totalCount,
                ActiveCount = activeCount,
                InactiveCount = totalCount - activeCount,
                DoctorCount = stats.FirstOrDefault(s => s.Role == UserRole.Doctor)?.Count ?? 0,
                NurseCount = stats.FirstOrDefault(s => s.Role == UserRole.Nurse)?.Count ?? 0,
                AdminCount = stats.FirstOrDefault(s => s.Role == UserRole.Admin)?.Count ?? 0,
                PharmacistCount = stats.FirstOrDefault(s => s.Role == UserRole.Pharmacist)?.Count ?? 0,
                ReceptionistCount = stats.FirstOrDefault(s => s.Role == UserRole.Receptionist)?.Count ?? 0
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
            user.UpdatedAt = DateTime.Now;
            
            Context.Entry(user).Property(u => u.PasswordHash).IsModified = true;
            Context.Entry(user).Property(u => u.UpdatedAt).IsModified = true;
            
            return await SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateLastLoginAsync(Guid userId, DateTime loginTime)
        {
            Logger.LogDebug("Updating last login time for user: {UserId}", userId);
            
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.LastLoginAt = loginTime;
            
            Context.Entry(user).Property(u => u.LastLoginAt).IsModified = true;
            
            return await SaveChangesAsync() > 0;
        }

        public async Task<int> GetActiveCountAsync()
        {
            Logger.LogDebug("Getting active user count");
            
            return await DbSet
                .AsNoTracking()
                .CountAsync(u => u.IsActive);
        }

        #endregion

        #region Protected Override Methods

        protected override IQueryable<UserModel> ApplySearch(IQueryable<UserModel> queryable, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return queryable;
            }

            var lowerSearchTerm = searchTerm.ToLower();
            
            return queryable.Where(u => 
                u.UserName.ToLower().Contains(lowerSearchTerm) ||
                u.RealName.ToLower().Contains(lowerSearchTerm) ||
                u.Email.ToLower().Contains(lowerSearchTerm) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(lowerSearchTerm)));
        }

        protected override IQueryable<UserModel> ApplyDefaultSorting(IQueryable<UserModel> queryable)
        {
            return queryable.OrderByDescending(u => u.CreatedAt)
                           .ThenBy(u => u.RealName);
        }

        #endregion
    }

    /// <summary>
    /// 用户统计DTO
    /// </summary>
    public class UserStatisticsDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int DoctorCount { get; set; }
        public int NurseCount { get; set; }
        public int AdminCount { get; set; }
        public int PharmacistCount { get; set; }
        public int ReceptionistCount { get; set; }
    }
}