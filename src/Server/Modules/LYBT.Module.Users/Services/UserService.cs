using LYBT.Entities.Users;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services;

/// <summary>
/// 用户管理服务实现
/// 供 WebAPI 和 LocalWebAPI 共享，减少 Controller 层业务逻辑
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    private static readonly string[] ReservedUsernames =
        { "admin", "administrator", "root", "system", "superadmin", "sysadmin" };

    public UserService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<UserService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<UserListDto>> GetPagedUsersAsync(
        int page, int pageSize, string? keyword,
        UserRole? role, CommonStatus? status,
        CancellationToken ct = default)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                (u.RealName != null && u.RealName.ToLower().Contains(kw)) ||
                (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
        }

        var totalCount = await query.CountAsync(ct);
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = BaseClaimsHelper.ParseUserRole(roles);
            var isEnabled = !await _userManager.GetLockoutEnabledAsync(user) ||
                await _userManager.GetAccessFailedCountAsync(user) < 5;

            if (role.HasValue && userRole != role.Value) continue;
            if (status.HasValue && isEnabled != (status.Value == CommonStatus.Enabled)) continue;

            dtos.Add(new UserListDto
            {
                Id = user.Id,
                UserName = user.UserName,
                RealName = user.RealName,
                PhoneNumber = user.PhoneNumber,
                Role = userRole,
                Status = isEnabled ? CommonStatus.Enabled : CommonStatus.Disabled,
                LastLoginTime = user.LastLoginAt,
                CreatedAt = DateTime.MinValue
            });
        }

        return new PagedResult<UserListDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return null;
        return await MapToDetailDtoAsync(user);
    }

    public async Task<UserDetailDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return null;
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;
        return await MapToDetailDtoAsync(user);
    }

    public async Task<(bool Success, UserDetailDto? User, string? Error)> CreateUserAsync(
        UserInputDto dto, Guid currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName))
            return (false, null, "用户名不能为空");

        if (ReservedUsernames.Any(r => string.Equals(dto.UserName, r, StringComparison.OrdinalIgnoreCase)))
            return (false, null, $"用户名 '{dto.UserName}' 为系统保留用户名，不可使用");

        var existing = await _userManager.FindByNameAsync(dto.UserName);
        if (existing != null)
            return (false, null, $"用户名 '{dto.UserName}' 已存在");

        var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
        var targetRole = dto.Role ?? UserRole.Doctor;

        if (!BaseClaimsHelper.CanManageUser(
            currentUser?.IsSysAdmin == true ? UserRole.SuperAdmin : GetUserRole(currentUser),
            targetRole, currentUser?.IsSysAdmin == true))
        {
            return (false, null, "您没有权限创建该角色的用户");
        }

        var appUser = new ApplicationUser
        {
            UserName = dto.UserName,
            RealName = dto.RealName ?? string.Empty,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            LastLoginAt = null
        };

        var password = !string.IsNullOrWhiteSpace(dto.Password)
            ? dto.Password
            : _configuration["DefaultPasswords:NewUserPassword"]
                ?? throw new InvalidOperationException("DefaultPasswords:NewUserPassword 配置缺失");

        var result = await _userManager.CreateAsync(appUser, password);
        if (!result.Succeeded)
            return (false, null, $"创建用户失败: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        var roleString = BaseClaimsHelper.MapUserRoleToString(targetRole);
        var roleResult = await _userManager.AddToRoleAsync(appUser, roleString);
        if (!roleResult.Succeeded)
            _logger.LogWarning("用户创建成功但角色分配失败: {Errors}", string.Join("; ", roleResult.Errors.Select(e => e.Description)));

        var dtoResult = await MapToDetailDtoAsync(appUser);
        return (true, dtoResult, null);
    }

    public async Task<(bool Success, UserDetailDto? User, string? Error)> UpdateUserAsync(
        Guid id, UserInputDto dto, Guid currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return (false, null, "用户不存在");

        if (user.IsSysAdmin) return (false, null, "系统管理员账号不可被修改");

        if (!string.IsNullOrWhiteSpace(dto.RealName)) user.RealName = dto.RealName;
        if (dto.Email != null) user.Email = dto.Email;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, null, $"更新用户失败: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        if (dto.Role.HasValue)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
            var currentUserRole = currentUser?.IsSysAdmin == true ? UserRole.SuperAdmin : GetUserRole(currentUser);

            if (!BaseClaimsHelper.CanManageUser(currentUserRole, dto.Role.Value, currentUser?.IsSysAdmin == true))
                return (false, null, "您没有权限将用户角色修改为该级别");

            var newRoleString = BaseClaimsHelper.MapUserRoleToString(dto.Role.Value);
            foreach (var existingRole in currentRoles)
            {
                if (!string.Equals(existingRole, newRoleString, StringComparison.OrdinalIgnoreCase))
                    await _userManager.RemoveFromRoleAsync(user, existingRole);
            }

            if (!await _userManager.IsInRoleAsync(user, newRoleString))
                await _userManager.AddToRoleAsync(user, newRoleString);
        }

        var dtoResult = await MapToDetailDtoAsync(user);
        return (true, dtoResult, null);
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(
        Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        if (currentUserId == id) return (false, "不能删除自己的账户");

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return (false, "用户不存在");

        if (user.IsSysAdmin) return (false, "系统管理员账号不可被删除");

        var roles = await _userManager.GetRolesAsync(user);
        var userRole = BaseClaimsHelper.ParseUserRole(roles);
        var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
        var currentUserRole = currentUser?.IsSysAdmin == true ? UserRole.SuperAdmin : GetUserRole(currentUser);

        if (!BaseClaimsHelper.CanManageUser(currentUserRole, userRole, currentUser?.IsSysAdmin == true))
            return (false, "您没有权限删除该用户");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return (false, $"删除用户失败: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        return (true, null);
    }

    public async Task<(bool Success, string? TemporaryPassword, string? Error)> ResetPasswordAsync(
        Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return (false, null, "用户不存在");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var newPassword = _configuration["DefaultPasswords:NewUserPassword"]
            ?? throw new InvalidOperationException("DefaultPasswords:NewUserPassword 配置缺失");

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            return (false, null, $"密码重置失败: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        return (true, newPassword, null);
    }

    public async Task<(bool Success, UserDetailDto? User, string? Error)> ChangeProfileAsync(
        Guid id, ChangeProfileDto dto, Guid currentUserId, CancellationToken ct = default)
    {
        if (id != currentUserId) return (false, null, "只能修改自己的个人资料");

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return (false, null, "用户不存在");

        user.RealName = dto.RealName;
        user.PhoneNumber = dto.PhoneNumber;
        if (dto.Email != null) user.Email = dto.Email;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, null, $"个人资料修改失败: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        var dtoResult = await MapToDetailDtoAsync(user);
        return (true, dtoResult, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid id, string oldPassword, string newPassword, Guid currentUserId, CancellationToken ct = default)
    {
        if (id != currentUserId) return (false, "只能修改自己的密码");

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return (false, "用户不存在");

        var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
        if (result.Succeeded) return (true, null);

        return (false, $"密码修改失败: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }

    public async Task<(bool Success, UserDetailDto? User, string? Error)> ToggleUserStatusAsync(
        Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return (false, null, "用户不存在");

        if (user.IsSysAdmin) return (false, null, "系统管理员账号不可被禁用");

        var roles = await _userManager.GetRolesAsync(user);
        var userRole = BaseClaimsHelper.ParseUserRole(roles);
        var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
        var currentUserRole = currentUser?.IsSysAdmin == true ? UserRole.SuperAdmin : GetUserRole(currentUser);

        if (!BaseClaimsHelper.CanManageUser(currentUserRole, userRole, currentUser?.IsSysAdmin == true))
            return (false, null, "您没有权限切换该用户状态");

        var isLocked = await _userManager.GetLockoutEnabledAsync(user);
        if (isLocked)
        {
            await _userManager.SetLockoutEnabledAsync(user, false);
            await _userManager.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }

        var dto = await MapToDetailDtoAsync(user);
        return (true, dto, null);
    }

    public async Task<BatchOperationResultDto> BatchDeleteUsersAsync(
        List<Guid> ids, Guid currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        var result = new BatchOperationResultDto { TotalCount = ids.Count };

        foreach (var id in ids)
        {
            if (id == currentUserId)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "不能删除自己" });
                result.FailureCount++;
                continue;
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "用户不存在" });
                result.FailureCount++;
                continue;
            }

            if (user.IsSysAdmin)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = "系统管理员账号不可被删除" });
                result.FailureCount++;
                continue;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = BaseClaimsHelper.ParseUserRole(roles);
            var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
            var currentUserRole = currentUser?.IsSysAdmin == true ? UserRole.SuperAdmin : GetUserRole(currentUser);

            if (!BaseClaimsHelper.CanManageUser(currentUserRole, userRole, currentUser?.IsSysAdmin == true))
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = "无权限删除" });
                result.FailureCount++;
                continue;
            }

            var deleteResult = await _userManager.DeleteAsync(user);
            if (deleteResult.Succeeded)
                result.SuccessCount++;
            else
            {
                var errors = string.Join("; ", deleteResult.Errors.Select(e => e.Description));
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = errors });
                result.FailureCount++;
            }
        }

        return result;
    }

    private async Task<UserDetailDto> MapToDetailDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var userRole = BaseClaimsHelper.ParseUserRole(roles);
        var isLocked = await _userManager.GetLockoutEnabledAsync(user);

        return new UserDetailDto
        {
            Id = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Role = userRole,
            Status = isLocked ? CommonStatus.Disabled : CommonStatus.Enabled,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            LastLoginTime = user.LastLoginAt,
            FailedLoginCount = await _userManager.GetAccessFailedCountAsync(user),
            CreatedAt = DateTime.MinValue,
            UpdatedAt = null
        };
    }

    private static UserRole GetUserRole(ApplicationUser? user)
    {
        if (user == null) return UserRole.Receptionist;
        if (user.IsSysAdmin) return UserRole.SuperAdmin;
        return UserRole.Doctor;
    }
}
