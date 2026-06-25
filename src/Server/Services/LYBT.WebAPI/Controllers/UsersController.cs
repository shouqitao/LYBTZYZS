using Asp.Versioning;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/users")]
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserManagerService _userManagerService;
        private readonly IConfiguration _configuration;

        public UsersController(IUserManagerService userManagerService, IConfiguration configuration, ILogger<UsersController> logger)
            : base(logger)
        {
            _userManagerService = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpGet]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserListDto>>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetList(
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var query = await _userManagerService.GetUsersAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                    (u.RealName != null && u.RealName.ToLower().Contains(kw)) ||
                    (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = new List<UserListDto>();
            foreach (var user in users)
            {
                var roles = await _userManagerService.GetRolesAsync(user);
                var userRole = ParseUserRole(roles);
                var isEnabled = !await _userManagerService.GetLockoutEnabledAsync(user) ||
                    await _userManagerService.GetAccessFailedCountAsync(user) < 5;

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

            var pagedResult = new PagedResult<UserListDto>(dtos, totalCount, page, pageSize);
            return SuccessPaged(pagedResult, "查询成功");
        }

        [HttpGet("current")]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("无法获取当前用户信息");
            }

            if (userId == Guid.Empty)
            {
                var username = User.Identity?.Name ?? "sysadmin";
                var isSuperAdmin = User.FindFirst("IsSuperAdmin")?.Value == "true";

                if (isSuperAdmin)
                {
                    var superAdminDto = new UserDetailDto
                    {
                        Id = Guid.Empty,
                        UserName = username,
                        RealName = "系统超级管理员",
                        Role = UserRole.Admin,
                        Email = _configuration["Lybt:SystemAdmin:Email"]
                            ?? throw new InvalidOperationException("未配置系统管理员Email: Lybt:SystemAdmin:Email"),
                        Status = CommonStatus.Enabled,
                        CreatedAt = DateTime.MinValue,
                        UpdatedAt = DateTime.UtcNow
                    };
                    return Success(superAdminDto);
                }
            }

            var user = await _userManagerService.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            var dto = await MapToDetailDtoAsync(user);
            return Success(dto);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var user = await _userManagerService.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            var dto = await MapToDetailDtoAsync(user);
            return Success(dto);
        }

        [HttpPost]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] UserInputDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.UserName == null)
            {
                return ValidationFail("用户名不能为空");
            }

            var reservedUsernames = new[] { "admin", "administrator", "root", "system", "superadmin", "sysadmin" };
            if (reservedUsernames.Any(reserved => string.Equals(dto.UserName, reserved, StringComparison.OrdinalIgnoreCase)))
            {
                return Error($"用户名 '{dto.UserName}' 为系统保留用户名，不可使用");
            }

            var existing = await _userManagerService.FindByNameAsync(dto.UserName);
            if (existing != null)
            {
                return Error($"用户名 '{dto.UserName}' 已存在");
            }

            var (_, _, currentRole) = GetOperator();
            var currentUser = await _userManagerService.FindByIdAsync(GetOperator().OperatorId);
            var targetRole = dto.Role ?? UserRole.Doctor;

            if (!CanManageUser(currentRole, targetRole, currentUser?.IsSysAdmin == true))
            {
                return Forbid("您没有权限创建该角色的用户");
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
                : _configuration["DefaultPasswords:NewUserPassword"] ?? "Lybt2025@TempPass!";

            var result = await _userManagerService.CreateAsync(appUser, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return Error($"创建用户失败: {errors}");
            }

            var roleString = MapUserRoleToString(targetRole);
            var roleResult = await _userManagerService.AddToRoleAsync(appUser, roleString);
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("用户创建成功但角色分配失败: {Errors}", string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }

            LogOperation("创建用户", dto, appUser.Id);
            var detailDto = await MapToDetailDtoAsync(appUser);
            return CreatedAtAction(nameof(GetById),
                new { id = appUser.Id, version = "1" },
                ApiResponse<UserDetailDto>.CreateSuccess(detailDto, "创建成功"));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UserInputDto dto)
        {
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var user = await _userManagerService.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            if (user.IsSysAdmin)
            {
                return Forbid("系统管理员账号不可被修改");
            }

            if (!string.IsNullOrWhiteSpace(dto.RealName))
            {
                user.RealName = dto.RealName;
            }
            if (dto.Email != null)
            {
                user.Email = dto.Email;
            }
            if (dto.PhoneNumber != null)
            {
                user.PhoneNumber = dto.PhoneNumber;
            }

            var result = await _userManagerService.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return Error($"更新用户失败: {errors}");
            }

            if (dto.Role.HasValue)
            {
                var currentRoles = await _userManagerService.GetRolesAsync(user);
                var (opId, _, currentRole) = GetOperator();
                var opUser = await _userManagerService.FindByIdAsync(opId);

                if (!CanManageUser(currentRole, dto.Role.Value, opUser?.IsSysAdmin == true))
                {
                    return Forbid("您没有权限将用户角色修改为该级别");
                }

                var newRoleString = MapUserRoleToString(dto.Role.Value);
                foreach (var existingRole in currentRoles)
                {
                    if (!string.Equals(existingRole, newRoleString, StringComparison.OrdinalIgnoreCase))
                    {
                        await _userManagerService.RemoveFromRoleAsync(user, existingRole);
                    }
                }

                if (!await _userManagerService.IsInRoleAsync(user, newRoleString))
                {
                    await _userManagerService.AddToRoleAsync(user, newRoleString);
                }
            }

            LogOperation("更新用户", dto, id);
            var dtoResult = await MapToDetailDtoAsync(user);
            return Success(dtoResult, "用户更新成功");
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var (currentUserId, _, currentRole) = GetOperator();
            if (currentUserId == id)
            {
                return Forbid("不能删除自己的账户");
            }

            var user = await _userManagerService.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            if (user.IsSysAdmin)
            {
                return Forbid("系统管理员账号不可被删除");
            }

            var roles = await _userManagerService.GetRolesAsync(user);
            var userRole = ParseUserRole(roles);
            var opUser = await _userManagerService.FindByIdAsync(currentUserId);
            if (!CanManageUser(currentRole, userRole, opUser?.IsSysAdmin == true))
            {
                return Forbid("您没有权限删除该用户");
            }

            var result = await _userManagerService.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return Error($"删除用户失败: {errors}");
            }

            LogOperation("删除用户", null, id);
            return Success("删除成功");
        }

        [HttpPost("{id:guid}/reset-password")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequestDto request)
        {
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var user = await _userManagerService.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            var token = await _userManagerService.GeneratePasswordResetTokenAsync(user);
            var newPassword = _configuration["DefaultPasswords:NewUserPassword"] ?? "Lybt2025@TempPass!";

            var result = await _userManagerService.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return Error($"密码重置失败: {errors}");
            }

            LogOperation("重置用户密码", new { AutoGenerated = true }, id);
            return Success(new ResetPasswordResponseDto
            {
                Success = true,
                TemporaryPassword = newPassword
            }, "密码重置成功");
        }

        [HttpPut("{id:guid}/profile")]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ChangeProfile(Guid id, [FromBody] ChangeProfileDto dto)
        {
            var (currentUserId, _, _) = GetOperator();
            if (id != currentUserId)
                return Forbid("只能修改自己的个人资料");

            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var user = await _userManagerService.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            user.RealName = dto.RealName;
            user.PhoneNumber = dto.PhoneNumber;
            if (dto.Email != null)
            {
                user.Email = dto.Email;
            }

            var result = await _userManagerService.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return Error($"个人资料修改失败: {errors}");
            }

            LogOperation("修改个人资料", new { RealName = dto.RealName, PhoneNumber = dto.PhoneNumber }, id);
            var detailDto = await MapToDetailDtoAsync(user);
            return Success(detailDto, "个人资料修改成功");
        }

        [HttpPut("{id:guid}/change-password")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request)
        {
            var (currentUserId, _, _) = GetOperator();
            if (id != currentUserId)
                return Forbid("只能修改自己的密码");

            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var user = await _userManagerService.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            var result = await _userManagerService.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (result.Succeeded)
            {
                LogOperation("修改密码", new { UserId = id }, id);
                return Success("密码修改成功");
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Error($"密码修改失败: {errors}");
        }

        [HttpPost("{id:guid}/toggle-status")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var (_, _, currentRole) = GetOperator();
            var user = await _userManagerService.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            if (user.IsSysAdmin)
            {
                return Forbid("系统管理员账号不可被禁用");
            }

            var roles = await _userManagerService.GetRolesAsync(user);
            var userRole = ParseUserRole(roles);
            var (opId, _, _) = GetOperator();
            var opUser = await _userManagerService.FindByIdAsync(opId);
            if (!CanManageUser(currentRole, userRole, opUser?.IsSysAdmin == true))
            {
                return Forbid("您没有权限切换该用户状态");
            }

            var isLocked = await _userManagerService.GetLockoutEnabledAsync(user);
            if (isLocked)
            {
                await _userManagerService.SetLockoutEnabledAsync(user, false);
                await _userManagerService.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                await _userManagerService.SetLockoutEnabledAsync(user, true);
                await _userManagerService.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }

            LogOperation("切换用户状态", new { NewStatus = isLocked ? CommonStatus.Enabled : CommonStatus.Disabled }, id);
            var dto = await MapToDetailDtoAsync(user);
            return Success(dto, $"用户已{(isLocked ? "启用" : "禁用")}");
        }

        [HttpPost("batch-delete")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个用户");
            }

            var (currentUserId, _, currentRole) = GetOperator();
            var result = new BatchOperationResultDto
            {
                TotalCount = dto.Ids.Count
            };

            foreach (var id in dto.Ids)
            {
                if (id == currentUserId)
                {
                    result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "不能删除自己" });
                    result.FailureCount++;
                    continue;
                }

                var user = await _userManagerService.FindByIdAsync(id);
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

                var roles = await _userManagerService.GetRolesAsync(user);
                var userRole = ParseUserRole(roles);
                var opUser = await _userManagerService.FindByIdAsync(currentUserId);
                if (!CanManageUser(currentRole, userRole, opUser?.IsSysAdmin == true))
                {
                    result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = "无权限删除" });
                    result.FailureCount++;
                    continue;
                }

                var deleteResult = await _userManagerService.DeleteAsync(user);
                if (deleteResult.Succeeded)
                {
                    result.SuccessCount++;
                }
                else
                {
                    var errors = string.Join("; ", deleteResult.Errors.Select(e => e.Description));
                    result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = errors });
                    result.FailureCount++;
                }
            }

            LogOperation("批量删除用户", new { Ids = dto.Ids, Result = result.Message }, null);
            return Success(result, result.Message);
        }

        private static bool CanManageUser(UserRole? currentUserRole, UserRole? targetUserRole, bool isSysAdmin = false)
        {
            if (isSysAdmin) return true;

            if (!currentUserRole.HasValue || !targetUserRole.HasValue)
                return false;

            return currentUserRole.Value switch
            {
                UserRole.SuperAdmin => true,
                UserRole.Admin => targetUserRole.Value is UserRole.Doctor or UserRole.Receptionist,
                _ => false
            };
        }

        private static UserRole ParseUserRole(IList<string> roles)
        {
            if (roles.Count == 0) return UserRole.Doctor;

            var roleStr = roles[0];
            if (string.Equals(roleStr, RoleConstants.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                return UserRole.SuperAdmin;
            if (string.Equals(roleStr, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
                return UserRole.Admin;
            if (string.Equals(roleStr, RoleConstants.Doctor, StringComparison.OrdinalIgnoreCase))
                return UserRole.Doctor;
            if (string.Equals(roleStr, RoleConstants.Receptionist, StringComparison.OrdinalIgnoreCase))
                return UserRole.Receptionist;

            return UserRole.Doctor;
        }

        private static string MapUserRoleToString(UserRole role) => role switch
        {
            UserRole.SuperAdmin => RoleConstants.SuperAdmin,
            UserRole.Admin => RoleConstants.Admin,
            UserRole.Doctor => RoleConstants.Doctor,
            UserRole.Receptionist => RoleConstants.Receptionist,
            _ => RoleConstants.Doctor
        };

        private async Task<UserDetailDto> MapToDetailDtoAsync(ApplicationUser user)
        {
            var roles = await _userManagerService.GetRolesAsync(user);
            var userRole = ParseUserRole(roles);
            var isLocked = await _userManagerService.GetLockoutEnabledAsync(user);

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
                FailedLoginCount = await _userManagerService.GetAccessFailedCountAsync(user),
                CreatedAt = DateTime.MinValue,
                UpdatedAt = null
            };
        }
    }
}
