using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Repositories;

/// <summary>
/// 用户仓储 - 本地模式实现 (SYNC-D02)
/// 通过 EF Core + LocalDbContext 直接访问 SQL Server LocalDB。
/// DI 工厂根据 IConnectionModeProvider 在本地模式下选择此实现。
/// </summary>
public sealed class LocalUserRepository : IUserRepository
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalUserRepository> _logger;
    private readonly LocalUserMapper _mapper = new();

    // SYNC-D02: 过渡态，默认重置密码
    private const string DefaultResetPassword = "Lybt@2026";

    public LocalUserRepository(
        LocalDbContext context,
        ILogger<LocalUserRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] User.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(u =>
                    u.UserName.Contains(keyword) ||
                    u.RealName.Contains(keyword) ||
                    (u.PinYinCode != null && u.PinYinCode.Contains(keyword)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var listDtos = items.Select(e =>
            {
                var dto = _mapper.ToDetailDto(e);
                return new UserListDto
                {
                    Id = dto.Id,
                    UserName = dto.UserName,
                    RealName = dto.RealName,
                    PhoneNumber = dto.PhoneNumber,
                    Role = dto.Role,
                    Status = dto.Status,
                    LastLoginTime = dto.LastLoginTime,
                    CreatedAt = dto.CreatedAt
                };
            }).ToList();

            return new PagedResult<UserListDto>
            {
                Items = listDtos,
                TotalCount = total,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.GetPaged failed");
            throw;
        }
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] User.GetById - Id={Id}", id);

            var entity = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] User.GetById - NotFound: {Id}", id);
                return null;
            }

            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO:Local] User.Create - UserName={UserName}", dto.UserName);

            // 检查用户名是否已存在
            var exists = await _context.Users
                .AnyAsync(u => u.UserName == dto.UserName);

            if (exists)
                throw new InvalidOperationException($"用户名已存在: {dto.UserName}");

            var entity = _mapper.ToEntity(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.Status = CommonStatus.Enabled;

            // 密码哈希处理
            if (!string.IsNullOrEmpty(dto.Password))
            {
                entity.PasswordHash = PasswordHelper.HashPassword(dto.Password, entity.Role, _logger);
            }

            _context.Users.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] User.Create completed - Id={Id}", entity.Id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.Create failed - UserName={UserName}", dto.UserName);
            throw;
        }
    }

    public async Task<UserDetailDto> UpdateAsync(UserInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var id = dto.Id ?? throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        try
        {
            _logger.LogInformation("[REPO:Local] User.Update - Id={Id}", id);

            var existing = await _context.Users.FindAsync(id)
                ?? throw new InvalidOperationException($"用户不存在: {id}");

            // 检查用户名是否被其他用户使用
            if (dto.UserName != null)
            {
                var nameConflict = await _context.Users
                    .AnyAsync(u => u.UserName == dto.UserName && u.Id != id);

                if (nameConflict)
                    throw new InvalidOperationException($"用户名已被使用: {dto.UserName}");
            }

            // 保留密码哈希 (不通过此方法更新密码)
            var passwordHash = existing.PasswordHash;

            // 更新可变字段
            if (dto.UserName != null) existing.UserName = dto.UserName;
            if (dto.RealName != null) existing.RealName = dto.RealName;
            existing.PinYinCode = dto.PinYinCode;
            existing.PhoneNumber = dto.PhoneNumber;
            existing.Email = dto.Email;
            if (dto.Role.HasValue) existing.Role = dto.Role.Value;
            existing.Remark = dto.Remark;
            existing.UpdatedAt = DateTime.UtcNow;

            // 确保密码不被覆盖
            existing.PasswordHash = passwordHash;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] User.Update completed - Id={Id}", id);
            return _mapper.ToDetailDto(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.Update failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] User.Delete - Id={Id}", id);

            var entity = await _context.Users.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] User.Delete - NotFound: {Id}", id);
                return false;
            }

            // SuperAdmin 保护
            if (entity.Role == UserRole.SuperAdmin)
            {
                _logger.LogWarning("[REPO:Local] User.Delete - Cannot delete SuperAdmin: {Id}", id);
                return false;
            }

            // 最后管理员保护
            if (entity.Role == UserRole.Admin && await IsLastAdminAsync(entity.Id))
            {
                _logger.LogWarning("[REPO:Local] User.Delete - Cannot delete last Admin: {Id}", id);
                return false;
            }

            entity.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] User.Delete completed - Id={Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<UserListDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] User.Search - Keyword={Keyword}", keyword);

            if (string.IsNullOrWhiteSpace(keyword))
                return [];

            var entities = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.UserName.Contains(keyword) ||
                    u.RealName.Contains(keyword) ||
                    (u.PinYinCode != null && u.PinYinCode.Contains(keyword)))
                .OrderBy(u => u.UserName)
                .Take(100)
                .ToListAsync();

            return entities.Select(e =>
            {
                var dto = _mapper.ToDetailDto(e);
                return new UserListDto
                {
                    Id = dto.Id,
                    UserName = dto.UserName,
                    RealName = dto.RealName,
                    PhoneNumber = dto.PhoneNumber,
                    Role = dto.Role,
                    Status = dto.Status,
                    LastLoginTime = dto.LastLoginTime,
                    CreatedAt = dto.CreatedAt
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.Search failed");
            throw;
        }
    }

    #endregion

    #region 用户专用方法

    public async Task<UserDetailDto> GetByUsernameAsync(string username)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] User.GetByUsername - Username={Username}", username);

            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("用户名不能为空");

            var entity = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (entity == null)
                throw new InvalidOperationException($"用户 {username} 不存在");

            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.GetByUsername failed - Username={Username}", username);
            throw;
        }
    }

    public async Task<List<UserListDto>> GetDoctorsAsync()
    {
        try
        {
            _logger.LogDebug("[REPO:Local] User.GetDoctors started");

            var entities = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var doctors = entities.Select(e =>
            {
                var dto = _mapper.ToDetailDto(e);
                return new UserListDto
                {
                    Id = dto.Id,
                    UserName = dto.UserName,
                    RealName = dto.RealName,
                    PhoneNumber = dto.PhoneNumber,
                    Role = dto.Role,
                    Status = dto.Status,
                    LastLoginTime = dto.LastLoginTime,
                    CreatedAt = dto.CreatedAt
                };
            }).ToList();

            _logger.LogInformation("[REPO:Local] User.GetDoctors completed - Count={Count}", doctors.Count);
            return doctors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.GetDoctors failed");
            return [];
        }
    }

    public Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
    {
        // 本地模式: 修改个人资料通过 UpdateAsync 实现
        _logger.LogWarning("[REPO:Local] User.ChangeProfile - 本地模式不支持修改个人资料");
        throw new NotSupportedException("本地模式不支持修改个人资料");
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] User.ChangePassword - UserId={UserId}", userId);

            var entity = await _context.Users.FindAsync(userId);
            if (entity == null)
                return ServiceResult.Failure("用户不存在");

            // 验证旧密码
            var verificationResult = PasswordHelper.VerifyPassword(request.OldPassword, entity.PasswordHash, entity.Role, _logger);
            if (!verificationResult.IsSuccess)
            {
                _logger.LogWarning("[REPO:Local] User.ChangePassword - Invalid old password: {UserId}", userId);
                return ServiceResult.Failure("旧密码不正确");
            }

            // 更新密码
            entity.PasswordHash = PasswordHelper.HashPassword(request.NewPassword, entity.Role, _logger);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] User.ChangePassword completed - UserId={UserId}", userId);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.ChangePassword failed - UserId={UserId}", userId);
            return ServiceResult.Failure("密码修改失败");
        }
    }

    public async Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequestDto request)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] User.ResetPassword - UserId={UserId}", userId);

            var entity = await _context.Users.FindAsync(userId);
            if (entity == null)
                return ServiceResult<ResetPasswordResponseDto>.Failure("用户不存在");

            entity.PasswordHash = PasswordHelper.HashPassword(DefaultResetPassword, entity.Role, _logger);
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] User.ResetPassword completed - UserId={UserId}", userId);
            return ServiceResult<ResetPasswordResponseDto>.Success(
                new ResetPasswordResponseDto { Success = true, TemporaryPassword = DefaultResetPassword });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.ResetPassword failed - UserId={UserId}", userId);
            return ServiceResult<ResetPasswordResponseDto>.Failure("重置密码失败");
        }
    }

    public Task<UserBatchImportResultDto?> BatchImportAsync(UserBatchImportInputDto request)
    {
        _logger.LogWarning("[REPO:Local] User.BatchImport - 本地模式不支持批量导入");
        return Task.FromResult<UserBatchImportResultDto?>(null);
    }

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<UserDetailDto?> ToggleStatusAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] User.ToggleStatus - Id={Id}", id);

            var entity = await _context.Users.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] User.ToggleStatus - NotFound: {Id}", id);
                return null;
            }

            // SuperAdmin 保护
            if (entity.Role == UserRole.SuperAdmin)
            {
                _logger.LogWarning("[REPO:Local] User.ToggleStatus - Cannot toggle SuperAdmin status: {Id}", id);
                return null;
            }

            // 禁用时检查最后管理员保护
            if (entity.Status == CommonStatus.Enabled &&
                entity.Role == UserRole.Admin &&
                await IsLastActiveAdminAsync(entity.Id))
            {
                _logger.LogWarning("[REPO:Local] User.ToggleStatus - Cannot disable last active Admin: {Id}", id);
                return null;
            }

            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] User.ToggleStatus completed - Id={Id}, Status={Status}",
                id, entity.Status);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.ToggleStatus failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<UserDetailDto?> RestoreAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] User.Restore - Id={Id}", id);

            // 使用 IgnoreQueryFilters 查找软删除的记录
            var entity = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] User.Restore - NotFound or not deleted: {Id}", id);
                return null;
            }

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] User.Restore completed - Id={Id}", id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] User.BatchDelete - Count={Count}", ids.Count);

            var result = new BatchOperationResultDto { TotalCount = ids.Count };

            foreach (var id in ids)
            {
                var success = await DeleteAsync(id);
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

            result.IsSuccess = result.FailureCount == 0;
            result.Message = $"批量删除完成: 成功 {result.SuccessCount}/{result.TotalCount}";

            _logger.LogInformation("[REPO:Local] User.BatchDelete completed - Success={Success}, Failure={Failure}",
                result.SuccessCount, result.FailureCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.BatchDelete failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] User.BatchEnable - Count={Count}", ids.Count);

            var result = new BatchOperationResultDto { TotalCount = ids.Count };

            foreach (var id in ids)
            {
                var entity = await _context.Users.FindAsync(id);
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
                    result.FailedItems.Add(new BatchOperationFailureItem
                        { Id = id, Name = entity.UserName, Reason = "不能修改超级管理员状态" });
                    continue;
                }

                entity.Status = CommonStatus.Enabled;
                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }

            await _context.SaveChangesAsync();
            result.IsSuccess = result.FailureCount == 0;
            result.Message = $"批量启用完成: 成功 {result.SuccessCount}/{result.TotalCount}";

            _logger.LogInformation("[REPO:Local] User.BatchEnable completed - Success={Success}, Failure={Failure}",
                result.SuccessCount, result.FailureCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.BatchEnable failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] User.BatchDisable - Count={Count}", ids.Count);

            var result = new BatchOperationResultDto { TotalCount = ids.Count };

            foreach (var id in ids)
            {
                var entity = await _context.Users.FindAsync(id);
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
                    result.FailedItems.Add(new BatchOperationFailureItem
                        { Id = id, Name = entity.UserName, Reason = "不能修改超级管理员状态" });
                    continue;
                }

                // 最后管理员保护
                if (entity.Role == UserRole.Admin && await IsLastActiveAdminAsync(entity.Id))
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                        { Id = id, Name = entity.UserName, Reason = "不能禁用最后一个管理员" });
                    continue;
                }

                entity.Status = CommonStatus.Disabled;
                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }

            await _context.SaveChangesAsync();
            result.IsSuccess = result.FailureCount == 0;
            result.Message = $"批量禁用完成: 成功 {result.SuccessCount}/{result.TotalCount}";

            _logger.LogInformation("[REPO:Local] User.BatchDisable completed - Success={Success}, Failure={Failure}",
                result.SuccessCount, result.FailureCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] User.BatchDisable failed");
            return null;
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>检查是否为最后一个未删除的 Admin</summary>
    private async Task<bool> IsLastAdminAsync(Guid excludeId)
    {
        var adminCount = await _context.Users
            .CountAsync(u => u.Role == UserRole.Admin && u.Id != excludeId && !u.IsDeleted);
        return adminCount == 0;
    }

    /// <summary>检查是否为最后一个启用状态的 Admin</summary>
    private async Task<bool> IsLastActiveAdminAsync(Guid excludeId)
    {
        var activeAdminCount = await _context.Users
            .CountAsync(u => u.Role == UserRole.Admin && u.Id != excludeId &&
                            !u.IsDeleted && u.Status == CommonStatus.Enabled);
        return activeAdminCount == 0;
    }

    #endregion
}
