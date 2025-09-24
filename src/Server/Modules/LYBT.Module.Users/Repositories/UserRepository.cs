using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Repositories
{

/// <summary>
/// 用户仓储实现类 - UltraThink优化版
/// 继承OptimizedBaseRepository提供高性能缓存CRUD，实现用户特定业务逻辑
/// 优化特性：查询缓存、批量操作、性能监控、智能缓存失效
/// 实现软删除策略：用户只能禁用/启用，不能物理删除
/// </summary>
public class UserRepository : OptimizedBaseRepository<User>, IUserRepository
{

public UserRepository(
AppDbContext dbContext,
ILogger<UserRepository> logger,
IMemoryCache cache)
: base(dbContext, logger, cache)
{
// OptimizedBaseRepository会处理基础的数据库操作和缓存策略
}

// 注意：AddAsync, UpdateAsync等基础CRUD方法由BaseRepository提供

/// <summary>
/// 禁用用户（软删除）- 缓存感知版
/// </summary>
public async Task<bool> DisableAsync(Guid id)
{
var user = await _dbSet.FindAsync(id);
if (user == null)
{
return false;
}

user.Status = CommonStatus.Disabled;
_dbSet.Update(user);
var result = await _context.SaveChangesAsync() > 0;

// 缓存失效
if (result)
{
_cache.Remove($"{CacheKeyPrefix}{id}");
InvalidateCache();
}

return result;
}

/// <summary>
/// 启用用户 - 缓存感知版
/// </summary>
public async Task<bool> EnableAsync(Guid id)
{
var user = await _dbSet.FindAsync(id);
if (user == null)
{
return false;
}

user.Status = CommonStatus.Enabled;
_dbSet.Update(user);
var result = await _context.SaveChangesAsync() > 0;

// 缓存失效
if (result)
{
_cache.Remove($"{CacheKeyPrefix}{id}");
InvalidateCache();
}

return result;
}

/// <summary>
/// 分页条件查找用户（缓存优化版）
/// 权限控制：禁用的用户仅管理员可查询
/// </summary>
public async Task<(IList<User> Users, int Total)> GetPagedAsync(UserSearchDto query, bool includeDisabled = false)
{
var cacheKey = GenerateCacheKey("paged", query.GetHashCode(), includeDisabled);

if (_cache.TryGetValue<(IList<User>, int)>(cacheKey, out var cached))
{
_logger.LogDebug("从缓存获取用户分页数据: {CacheKey}", cacheKey);
return cached;
}

var dbSet = _dbSet.AsQueryable();

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
(u.PinYinCode != null && u.PinYinCode.Contains(keyword.ToUpperInvariant())));
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

// UltraThink v2.0: 时间字段已删除（CreateTime, LastLoginTime）
// 日期范围筛选功能已简化移除，相关查询条件将被忽略

// 分页查询 - UltraThink v2.0: 使用用户名排序曷代已删除的CreateTime
int skip = (query.PageIndex - 1) * query.PageSize;
var users = await dbSet
.OrderByDescending(u => u.CreatedAt) // 按创建时间降序排序
.Skip(skip)
.Take(query.PageSize)
.ToListAsync();

var result = (Users: users, Total: total);

// 缓存结果
SetCacheSafely(cacheKey, result, DefaultCacheDuration);

return result;
}

/// <summary>
/// 根据用户名查找（包括禁用用户，用于登录验证）- 缓存优化版
/// </summary>
public async Task<User?> GetByUsernameAsync(string userName)
{
var cacheKey = GenerateCacheKey("username", userName);

if (_cache.TryGetValue<User?>(cacheKey, out var cached))
{
return cached;
}

var user = await _dbSet
.AsNoTracking()
.FirstOrDefaultAsync(u => u.Username == userName);

SetCacheSafely(cacheKey, user, DefaultCacheDuration);
return user;
}

/// <summary>
/// 根据ID查找 - 缓存优化版
/// 权限控制：禁用的用户仅管理员可查询
/// </summary>
public async Task<User?> GetByIdAsync(Guid id, bool includeDisabled = false)
{
var cacheKey = GenerateCacheKey("byId", id, includeDisabled);

if (_cache.TryGetValue<User?>(cacheKey, out var cached))
{
return cached;
}

var query = _dbSet.AsNoTracking();

if (!includeDisabled)
{
query = query.Where(u => u.Status == CommonStatus.Enabled);
}

var user = await query.FirstOrDefaultAsync(u => u.Id == id);
SetCacheSafely(cacheKey, user, DefaultCacheDuration);
return user;
}

/// <summary>
/// 根据ID列表批量获取用户 - 缓存优化版
/// 权限控制：禁用的用户仅管理员可查询
/// 使用OptimizedBaseRepository的批量缓存功能
/// </summary>
public async Task<List<User>> GetUsersByIdsAsync(List<Guid> ids, bool includeDisabled = false)
{
if (!ids.Any())
{
return new List<User>();
}

// 使用OptimizedBaseRepository的批量查询功能
var batchResult = await GetByIdsAsync(ids);
var users = batchResult.Values.ToList();

if (!includeDisabled)
{
users = users.Where(u => u.Status == CommonStatus.Enabled).ToList();
}

return users;
}

/// <summary>
/// 校验用户名是否存在（包括禁用用户）- 缓存优化版
/// </summary>
public async Task<bool> ExistsByUsernameAsync(string userName)
{
var cacheKey = GenerateCacheKey("exists", userName);

if (_cache.TryGetValue<bool>(cacheKey, out var cached))
{
return cached;
}

var exists = await _dbSet.AsNoTracking().AnyAsync(u => u.Username == userName);
SetCacheSafely(cacheKey, exists, DefaultCacheDuration);
return exists;
}

        /// <summary>
        /// 检查手机号是否已存在
        /// </summary>
        public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            var cacheKey = $"user:exists:phone:{phoneNumber}";
            
            // 尝试从缓存获取
            if (_cache.TryGetValue<bool>(cacheKey, out var exists))
            {
                return exists;
            }

            // 查询数据库
            exists = await _dbSet
                .AnyAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted);
            
            // 设置缓存
            SetCacheSafely(cacheKey, exists, DefaultCacheDuration);
            
            return exists;
        }

/// <summary>
/// 更新用户密码 - 缓存感知版
/// </summary>
public async Task<bool> UpdatePasswordAsync(Guid id, string passwordHash)
{
var user = await _dbSet.FindAsync(id);
if (user == null)
{
return false;
}

user.PasswordHash = passwordHash;
_dbSet.Update(user);
var result = await _context.SaveChangesAsync() > 0;

// 缓存失效
if (result)
{
_cache.Remove($"{CacheKeyPrefix}{id}");
InvalidateCache();
}

return result;
}

/// <summary>
/// 批量更新启用状态 - 缓存感知版
/// </summary>
public async Task<int> UpdateActiveStatusAsync(List<Guid> ids, bool isActive)
{
if (!ids.Any())
{
return 0;
}

            var status = isActive ? CommonStatus.Enabled : CommonStatus.Disabled;
            int result;

            try
            {
                // 优先使用ExecuteUpdateAsync（生产环境）
                result = await _dbSet
                    .Where(u => ids.Contains(u.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.Status, status));
            }
            catch (InvalidOperationException)
            {
                // InMemory数据库回退逻辑
                var users = await _dbSet
                    .Where(u => ids.Contains(u.Id))
                    .ToListAsync();

                foreach (var user in users)
                {
                    user.Status = status;
                    _context.Entry(user).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                result = users.Count; // 返回总数，与ExecuteUpdateAsync行为一致
            }

// 批量缓存失效
if (result > 0)
{
foreach (var id in ids)
{
_cache.Remove($"{CacheKeyPrefix}{id}");
}

InvalidateCache();
}

return result;
}

/// <summary>
/// 获取启用的用户列表 - 缓存优化版
/// </summary>
public async Task<List<User>> GetActiveUsersAsync()
{
var cacheKey = GenerateCacheKey("active_users");

if (_cache.TryGetValue<List<User>>(cacheKey, out var cached))
{
return cached!;
}

var users = await _dbSet
.AsNoTracking()
.Where(u => u.Status == CommonStatus.Enabled)
.OrderBy(u => u.RealName)
.ToListAsync();

SetCacheSafely(cacheKey, users, DefaultCacheDuration);
return users;
}

// 注意：GetAllAsync方法由BaseRepository提供
}
}
