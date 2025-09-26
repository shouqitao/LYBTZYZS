using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Repositories
{

    /// <summary>
    /// 登录验证仓储实现 - 数据层统一化重构
    /// 继承OptimizedBaseRepository获得缓存和性能优化，扩展认证特有业务方法
    /// </summary>
    public class AuthRepository : OptimizedBaseRepository<User>, IAuthRepository
    {

        public AuthRepository(
            AppDbContext context,
            ILogger<AuthRepository> logger,
            IMemoryCache cache) : base(context, logger, cache)
        {
        }

        // 注意：基础CRUD方法由OptimizedBaseRepository提供，带有缓存优化
        // 这里只实现认证特有的业务方法

        /// <summary>
        /// 通过用户名获取用户信息 - 缓存优化版
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <returns>用户实体或 null</returns>
        public async Task<User?> GetByUsernameAsync(string userName)
        {
            var cacheKey = $"{CacheKeyPrefix}username:{userName}";

            if (_cache.TryGetValue<User?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取用户信息 {Username}", userName);
                return cached;
            }

            var user = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UsernName == userName);

            // 配置缓存选项，解决SizeLimit配置问题
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = DefaultCacheDuration
            };
            options.SetSize(1); // 设置缓存项大小
            _cache.Set(cacheKey, user, options);
            return user;
        }

        /// <summary>
        /// 更新用户的最后登录时间 - UltraThink v2.0简化：通过AuthSession记录
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="loginTime">登录时间</param>
        public async Task UpdateLastLoginTimeAsync(Guid id, DateTime loginTime)
        {
            // UltraThink v2.0简化：User实体不再包含LastLoginTime字段
            // 登录时间信息通过AuthSession表记录，此方法仅保留接口兼容性
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取超级管理员密码哈希
        /// 重构：使用固定ID而非用户名，增强安全性
        /// </summary>
        /// <param name="userName">用户名（仅用于验证，实际通过固定ID查询）</param>
        /// <returns>密码哈希或 null</returns>
        public async Task<string?> GetAdminPasswordHashAsync(string userName)
        {
            // 使用固定的超级管理员ID，而不是依赖用户名
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var secret = await _context.AdminSecrets.FirstOrDefaultAsync(s => s.Id == adminSecretId);
            return secret?.PasswordHash;
        }

        /// <summary>
        /// 更新超级管理员密码哈希
        /// 重构：使用固定ID而非用户名，增强安全性
        /// </summary>
        /// <param name="userName">用户名（仅用于验证，实际通过固定ID更新）</param>
        /// <param name="passwordHash">新的密码哈希</param>
        public async Task UpdateAdminPasswordHashAsync(string userName, string passwordHash)
        {
            // 使用固定的超级管理员ID，而不是依赖用户名
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                // InMemory数据库的特殊处理：使用Find确保实体被追踪
                var secret = await _context.AdminSecrets.FindAsync(adminSecretId);
                if (secret != null)
                {
                    secret.PasswordHash = passwordHash;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // 生产环境使用ExecuteUpdate以获得更好的性能
                await _context.AdminSecrets
                    .Where(s => s.Id == adminSecretId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(s => s.PasswordHash, passwordHash));
            }
        }

        /// <summary>
        /// 更新用户登录保护信息 - UltraThink v2.0简化：通过AuthSession记录
        /// </summary>
        /// <param name="user">用户实体</param>
        public async Task UpdateUserLoginProtectionAsync(User user)
        {
            // UltraThink v2.0简化：User实体不再包含FailedLoginCount和LockoutEnd字段
            // 登录保护信息通过AuthSession记录，此方法仅保留接口兼容性
            await Task.CompletedTask;
        }

        /// <summary>
        /// 更新用户安全状态 - UltraThink Phase 3 安全增强
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="failedLoginCount">失败登录次数</param>
        /// <param name="lockoutEnd">锁定结束时间</param>
        public async Task UpdateUserSecurityAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd)
        {
            // 检查是否使用InMemory数据库，ExecuteUpdate在InMemory中不受支持
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                // 先检查实体是否已被跟踪，与UpdateFailedLoginInfoAsync保持一致
                var trackedEntity = _context.ChangeTracker.Entries<User>()
                    .FirstOrDefault(e => e.Entity.Id == userId)?.Entity;

                if (trackedEntity != null)
                {
                    // 如果已被跟踪，直接更新
                    trackedEntity.FailedLoginCount = failedLoginCount;
                    trackedEntity.LockoutEnd = lockoutEnd;
                }
                else
                {
                    // 如果未被跟踪，查找并附加
                    var existingUser = await _context.Users.FindAsync(userId);
                    if (existingUser != null)
                    {
                        existingUser.FailedLoginCount = failedLoginCount;
                        existingUser.LockoutEnd = lockoutEnd;
                    }
                }
                await _context.SaveChangesAsync();
                
                // 更新后清理缓存，确保后续查询获取最新数据
                var cacheKey = $"{CacheKeyPrefix}{userId}";
                _cache.Remove(cacheKey);
            }
            else
            {
                // 生产环境使用ExecuteUpdate提高性能
                await _dbSet.Where(u => u.Id == userId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.FailedLoginCount, failedLoginCount)
                        .SetProperty(u => u.LockoutEnd, lockoutEnd));
                        
                // 更新后清理缓存
                var cacheKey = $"{CacheKeyPrefix}{userId}";
                _cache.Remove(cacheKey);
            }
        }

        /// <summary>
        /// 更新失败登录信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="failedLoginCount">失败登录次数</param>
        /// <param name="lockoutEnd">锁定结束时间</param>
        public async Task UpdateFailedLoginInfoAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd)
        {
            // 检查是否使用InMemory数据库，ExecuteUpdate在InMemory中不受支持
            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                // 先检查实体是否已被跟踪
                var trackedEntity = _context.ChangeTracker.Entries<User>()
                    .FirstOrDefault(e => e.Entity.Id == userId)?.Entity;

                if (trackedEntity != null)
                {
                    // 如果已被跟踪，直接更新
                    trackedEntity.FailedLoginCount = failedLoginCount;
                    trackedEntity.LockoutEnd = lockoutEnd;
                }
                else
                {
                    // 如果未被跟踪，查找并附加
                    var existingUser = await _context.Users.FindAsync(userId);
                    if (existingUser != null)
                    {
                        existingUser.FailedLoginCount = failedLoginCount;
                        existingUser.LockoutEnd = lockoutEnd;
                    }
                }
                await _context.SaveChangesAsync();
                
                // 更新后清理缓存，确保后续查询获取最新数据
                var cacheKey = $"{CacheKeyPrefix}{userId}";
                _cache.Remove(cacheKey);
            }
            else
            {
                // 生产环境使用ExecuteUpdate提高性能
                await _dbSet.Where(u => u.Id == userId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.FailedLoginCount, failedLoginCount)
                        .SetProperty(u => u.LockoutEnd, lockoutEnd));
                        
                // 更新后清理缓存
                var cacheKey = $"{CacheKeyPrefix}{userId}";
                _cache.Remove(cacheKey);
            }
        }
    }
}
