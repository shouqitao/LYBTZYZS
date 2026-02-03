using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.DataSources;

/// <summary>
/// 本地用户数据源实现 - SQLite EF Core
/// OpenSpec: implement-local-mode
/// </summary>
public class LocalUserDataSource : IUserDataSource
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalUserDataSource> _logger;

    public LocalUserDataSource(LocalDbContext context, ILogger<LocalUserDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.GetById - Id={Id}", id);
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.GetByUsername - Username={Username}", username);

        if (string.IsNullOrWhiteSpace(username))
            return null;

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == username, ct);
    }

    public async Task<(List<User> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.GetPaged - Page={Page}, Keyword={Keyword}", page, keyword);

        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u =>
                u.UserName.Contains(keyword) ||
                u.RealName.Contains(keyword) ||
                (u.PinYinCode != null && u.PinYinCode.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<User> CreateAsync(User entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.Create - Username={Username}", entity.UserName);

        // 检查用户名是否已存在
        var exists = await _context.Users
            .AnyAsync(u => u.UserName == entity.UserName, ct);

        if (exists)
            throw new InvalidOperationException($"用户名已存在: {entity.UserName}");

        entity.Id = Guid.NewGuid();
        _context.Users.Add(entity);
        await _context.SaveChangesAsync(ct);

        return entity;
    }

    public async Task<User> UpdateAsync(User entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.Update - Id={Id}", entity.Id);

        var existing = await _context.Users.FindAsync([entity.Id], ct)
            ?? throw new InvalidOperationException($"用户不存在: {entity.Id}");

        // 检查用户名是否被其他用户使用
        var nameConflict = await _context.Users
            .AnyAsync(u => u.UserName == entity.UserName && u.Id != entity.Id, ct);

        if (nameConflict)
            throw new InvalidOperationException($"用户名已被使用: {entity.UserName}");

        // 保留密码哈希（不通过此方法更新密码）
        var passwordHash = existing.PasswordHash;
        _context.Entry(existing).CurrentValues.SetValues(entity);
        existing.PasswordHash = passwordHash;

        await _context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.Delete - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        // 不允许删除管理员
        if (entity.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("[LocalDataSource] User.Delete - Cannot delete SuperAdmin: {Id}", id);
            return false;
        }

        entity.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(
        Guid id,
        string oldPasswordHash,
        string newPasswordHash,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.ChangePassword - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        // 验证旧密码
        if (!BCrypt.Net.BCrypt.Verify(oldPasswordHash, entity.PasswordHash))
        {
            _logger.LogWarning("[LocalDataSource] User.ChangePassword - Invalid old password: {Id}", id);
            return false;
        }

        // 更新密码
        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPasswordHash);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] User.ChangePassword succeeded - Id={Id}", id);
        return true;
    }

    public async Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.ToggleStatus - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        // 不允许禁用管理员
        if (entity.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("[LocalDataSource] User.ToggleStatus - Cannot toggle SuperAdmin status: {Id}", id);
            return false;
        }

        entity.Status = entity.Status == CommonStatus.Enabled
            ? CommonStatus.Disabled
            : CommonStatus.Enabled;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateLastLoginTimeAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.UpdateLastLoginTime - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.LastLoginTime = DateTime.Now;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ResetFailedLoginCountAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.ResetFailedLoginCount - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.FailedLoginCount = 0;
        entity.LockoutEnd = null;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> IncrementFailedLoginCountAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.IncrementFailedLoginCount - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return 0;

        entity.FailedLoginCount++;
        await _context.SaveChangesAsync(ct);
        return entity.FailedLoginCount;
    }
}
