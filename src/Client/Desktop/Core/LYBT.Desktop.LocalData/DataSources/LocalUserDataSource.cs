using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
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
    private readonly LocalUserMapper _mapper = new();

    private readonly ICurrentUserProvider _currentUserProvider;

    // OpenSpec: SYNC-D02 - 过渡态，默认重置密码
    private const string DefaultResetPassword = "Lybt@2026";

    public LocalUserDataSource(
        LocalDbContext context,
        ILogger<LocalUserDataSource> logger,
        ICurrentUserProvider currentUserProvider)
    {
        _context = context;
        _logger = logger;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.GetById - Id={Id}", id);
        var entity = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<UserDetailDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.GetByUsername - Username={Username}", username);

        if (string.IsNullOrWhiteSpace(username))
            return null;

        var entity = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == username, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<(List<UserDetailDto> Items, int Total)> GetPagedAsync(
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

        return (items.Select(e => _mapper.ToDetailDto(e)).ToList(), total);
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.Create - Username={Username}", input.UserName);

        // 检查用户名是否已存在
        var exists = await _context.Users
            .AnyAsync(u => u.UserName == input.UserName, ct);

        if (exists)
            throw new InvalidOperationException($"用户名已存在: {input.UserName}");

        var entity = _mapper.ToEntity(input);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        entity.Status = CommonStatus.Enabled;

        // 密码哈希处理
        if (!string.IsNullOrEmpty(input.Password))
        {
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password);
        }

        _context.Users.Add(entity);
        await _context.SaveChangesAsync(ct);

        return _mapper.ToDetailDto(entity);
    }

    public async Task<UserDetailDto> UpdateAsync(UserInputDto input, CancellationToken ct = default)
    {
        var id = input.Id ?? throw new InvalidOperationException("更新用户时必须提供ID");
        _logger.LogInformation("[LocalDataSource] User.Update - Id={Id}", id);

        var existing = await _context.Users.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"用户不存在: {id}");

        // 检查用户名是否被其他用户使用
        if (input.UserName != null)
        {
            var nameConflict = await _context.Users
                .AnyAsync(u => u.UserName == input.UserName && u.Id != id, ct);

            if (nameConflict)
                throw new InvalidOperationException($"用户名已被使用: {input.UserName}");
        }

        // 保留密码哈希（不通过此方法更新密码）
        var passwordHash = existing.PasswordHash;

        // 更新可变字段
        if (input.UserName != null) existing.UserName = input.UserName;
        if (input.RealName != null) existing.RealName = input.RealName;
        existing.PinYinCode = input.PinYinCode;
        existing.PhoneNumber = input.PhoneNumber;
        existing.Email = input.Email;
        if (input.Role.HasValue) existing.Role = input.Role.Value;
        existing.Remark = input.Remark;
        existing.UpdatedAt = DateTime.Now;

        // 确保密码不被覆盖
        existing.PasswordHash = passwordHash;

        await _context.SaveChangesAsync(ct);
        return _mapper.ToDetailDto(existing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.Delete - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        // T4-X2-02: SuperAdmin 保护
        if (entity.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("[LocalDataSource] User.Delete - Cannot delete SuperAdmin: {Id}", id);
            return false;
        }

        // T4-X2-02: 最后管理员保护
        if (entity.Role == UserRole.Admin && await IsLastAdminAsync(entity.Id, ct))
        {
            _logger.LogWarning("[LocalDataSource] User.Delete - Cannot delete last Admin: {Id}", id);
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

        // T4-X2-06: SuperAdmin 保护
        if (entity.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("[LocalDataSource] User.ToggleStatus - Cannot toggle SuperAdmin status: {Id}", id);
            return false;
        }

        // T4-X2-06: 禁用时检查最后管理员保护
        if (entity.Status == CommonStatus.Enabled &&
            entity.Role == UserRole.Admin &&
            await IsLastActiveAdminAsync(entity.Id, ct))
        {
            _logger.LogWarning("[LocalDataSource] User.ToggleStatus - Cannot disable last active Admin: {Id}", id);
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

    // ==================== Sprint 4 X2 扩展方法 ====================
    // OpenSpec: SYNC-D02 - 过渡态方法

    /// <summary>T4-X2-03: 恢复已删除的用户</summary>
    public async Task<UserDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.Restore - Id={Id}", id);

        // 使用 IgnoreQueryFilters 查找软删除的记录
        var entity = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted, ct);

        if (entity == null)
        {
            _logger.LogWarning("[LocalDataSource] User.Restore - NotFound or not deleted: {Id}", id);
            return null;
        }

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] User.Restore succeeded - Id={Id}", id);
        return _mapper.ToDetailDto(entity);
    }

    /// <summary>T4-X2-04: 批量删除（逐个检查保护条件）</summary>
    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.BatchDelete - Count={Count}", ids.Count);

        var result = new BatchOperationResultDto { TotalCount = ids.Count };

        foreach (var id in ids)
        {
            try
            {
                var success = await DeleteAsync(id, ct);
                if (success)
                {
                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                }
                else
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "用户不存在或为受保护账户"
                    });
                }
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedIds.Add(id);
                result.FailedItems.Add(new BatchOperationFailureItem
                {
                    Id = id,
                    Reason = ex.Message
                });
            }
        }

        result.IsSuccess = result.FailureCount == 0;
        result.Message = $"批量删除完成: 成功 {result.SuccessCount}/{result.TotalCount}";
        return result;
    }

    /// <summary>T4-X2-05: 重置密码为默认密码</summary>
    public async Task<ResetPasswordResponseDto> ResetPasswordAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.ResetPassword - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
        {
            return new ResetPasswordResponseDto { Success = false, TemporaryPassword = string.Empty };
        }

        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultResetPassword);
        entity.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] User.ResetPassword succeeded - Id={Id}", id);
        return new ResetPasswordResponseDto { Success = true, TemporaryPassword = DefaultResetPassword };
    }

    /// <summary>T4-X2-07: 批量切换状态（逐个检查保护条件）</summary>
    public async Task<BatchOperationResultDto> BatchToggleStatusAsync(List<Guid> ids, bool enable, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.BatchToggleStatus - Count={Count}, Enable={Enable}", ids.Count, enable);

        var result = new BatchOperationResultDto { TotalCount = ids.Count };

        foreach (var id in ids)
        {
            try
            {
                var entity = await _context.Users.FindAsync([id], ct);
                if (entity == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "用户不存在" });
                    continue;
                }

                // SuperAdmin 保护
                if (entity.Role == UserRole.SuperAdmin)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = entity.UserName, Reason = "不能修改超级管理员状态" });
                    continue;
                }

                // 禁用时检查最后管理员
                if (!enable && entity.Role == UserRole.Admin && await IsLastActiveAdminAsync(entity.Id, ct))
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = entity.UserName, Reason = "不能禁用最后一个管理员" });
                    continue;
                }

                entity.Status = enable ? CommonStatus.Enabled : CommonStatus.Disabled;
                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedIds.Add(id);
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = ex.Message });
            }
        }

        await _context.SaveChangesAsync(ct);
        result.IsSuccess = result.FailureCount == 0;
        result.Message = $"批量{(enable ? "启用" : "禁用")}完成: 成功 {result.SuccessCount}/{result.TotalCount}";
        return result;
    }

    /// <summary>T4-X2-08: 获取当前用户</summary>
    public async Task<UserDetailDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.GetCurrentUser");

        var currentUserId = _currentUserProvider.CurrentUserId;
        if (currentUserId == null || currentUserId == Guid.Empty)
            return null;

        return await GetByIdAsync(currentUserId.Value, ct);
    }

    // ==================== 辅助方法 ====================

    /// <summary>检查是否为最后一个未删除的 Admin</summary>
    private async Task<bool> IsLastAdminAsync(Guid excludeId, CancellationToken ct)
    {
        var adminCount = await _context.Users
            .CountAsync(u => u.Role == UserRole.Admin && u.Id != excludeId && !u.IsDeleted, ct);
        return adminCount == 0;
    }

    /// <summary>检查是否为最后一个启用状态的 Admin</summary>
    private async Task<bool> IsLastActiveAdminAsync(Guid excludeId, CancellationToken ct)
    {
        var activeAdminCount = await _context.Users
            .CountAsync(u => u.Role == UserRole.Admin && u.Id != excludeId &&
                            !u.IsDeleted && u.Status == CommonStatus.Enabled, ct);
        return activeAdminCount == 0;
    }
}
