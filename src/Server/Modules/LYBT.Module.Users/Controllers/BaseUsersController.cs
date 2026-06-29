using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Controllers;

/// <summary>
/// 用户管理 Controller 共享基类
/// 供 WebAPI 和 LocalWebAPI 共享，减少重复代码
/// </summary>
using LYBT.Infrastructure.Constants;

[ApiController]
[Authorize]
public abstract class BaseUsersController : BaseApiController
{
    private readonly IUserService _userService;

    protected BaseUsersController(IUserService userService, ILogger logger)
        : base(logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <summary>
    /// 分页查询用户列表
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserListDto>>), 200)]
    [ProducesResponseType(400)]
    public virtual async Task<IActionResult> GetList(
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        UserRole? role = null,
        CommonStatus? status = null)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;

        var result = await _userService.GetPagedUsersAsync(page, pageSize, keyword, role, status, cancellationToken);
        return SuccessPaged(result, "查询成功");
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(401)]
    public virtual async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userId = BaseClaimsHelper.GetCurrentUserId(User);
        if (userId == Guid.Empty)
        {
            return Unauthorized("无法获取当前用户信息");
        }

        var dto = await _userService.GetCurrentUserAsync(userId, cancellationToken);
        if (dto == null)
        {
            return NotFound("用户不存在");
        }

        return Success(dto);
    }

    /// <summary>
    /// 根据 ID 获取用户详情
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var dto = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (dto == null)
        {
            return NotFound("用户不存在");
        }

        return Success(dto);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 201)]
    [ProducesResponseType(400)]
    public virtual async Task<IActionResult> Create([FromBody] UserInputDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var (success, user, error) = await _userService.CreateUserAsync(dto, currentUserId, isAdmin, cancellationToken);

        if (!success)
        {
            return Error(error ?? "创建用户失败");
        }

        LogOperation("创建用户", dto, user!.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = user.Id, version = ApiVersionConstants.V1 },
            ApiResponse<UserDetailDto>.CreateSuccess(user, "创建成功"));
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] UserInputDto dto)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var (success, user, err) = await _userService.UpdateUserAsync(id, dto, currentUserId, isAdmin);

        if (!success)
        {
            if (err == "用户不存在") return NotFound(err);
            if (err?.StartsWith("您没有权限") == true) return Forbid(err);
            if (err?.StartsWith("系统管理员") == true) return Forbid(err);
            return Error(err ?? "更新用户失败");
        }

        LogOperation("更新用户", dto, id);
        return Success(user!, "用户更新成功");
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> Delete(Guid id)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var (success, err) = await _userService.DeleteUserAsync(id, currentUserId, isAdmin);

        if (!success)
        {
            if (err == "用户不存在") return NotFound(err);
            if (err?.StartsWith("不能删除") == true) return Forbid(err);
            if (err?.StartsWith("系统管理员") == true) return Forbid(err);
            if (err?.StartsWith("您没有权限") == true) return Forbid(err);
            return Error(err ?? "删除用户失败");
        }

        LogOperation("删除用户", null, id);
        return Success("删除成功");
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponseDto>), 200)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequestDto request)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (success, temporaryPassword, err) = await _userService.ResetPasswordAsync(id);

        if (!success)
        {
            if (err == "用户不存在") return NotFound(err);
            return Error(err ?? "密码重置失败");
        }

        LogOperation("重置用户密码", new { AutoGenerated = true }, id);
        return Success(new ResetPasswordResponseDto
        {
            Success = true,
            TemporaryPassword = temporaryPassword
        }, "密码重置成功");
    }

    /// <summary>
    /// 修改个人资料
    /// </summary>
    [HttpPut("{id:guid}/profile")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> ChangeProfile(Guid id, [FromBody] ChangeProfileDto dto)
    {
        var (currentUserId, _, _) = GetOperator();

        var (success, user, err) = await _userService.ChangeProfileAsync(id, dto, currentUserId);

        if (!success)
        {
            if (err?.StartsWith("只能修改") == true) return Forbid(err);
            if (err == "用户不存在") return NotFound(err);
            return Error(err ?? "个人资料修改失败");
        }

        LogOperation("修改个人资料", new { RealName = dto.RealName, PhoneNumber = dto.PhoneNumber }, id);
        return Success(user!, "个人资料修改成功");
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPut("{id:guid}/change-password")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> ChangePassword(Guid id, [FromBody] LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request)
    {
        var (currentUserId, _, _) = GetOperator();

        var (success, err) = await _userService.ChangePasswordAsync(id, request.OldPassword, request.NewPassword, currentUserId);

        if (!success)
        {
            if (err?.StartsWith("只能修改") == true) return Forbid(err);
            if (err == "用户不存在") return NotFound(err);
            return Error(err ?? "密码修改失败");
        }

        LogOperation("修改密码", new { UserId = id }, id);
        return Success("密码修改成功");
    }

    /// <summary>
    /// 切换用户状态
    /// </summary>
    [HttpPost("{id:guid}/toggle-status")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public virtual async Task<IActionResult> ToggleStatus(Guid id)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var (success, user, err) = await _userService.ToggleUserStatusAsync(id, currentUserId, isAdmin);

        if (!success)
        {
            if (err == "用户不存在") return NotFound(err);
            if (err?.StartsWith("系统管理员") == true) return Forbid(err);
            if (err?.StartsWith("您没有权限") == true) return Forbid(err);
            return Error(err ?? "切换用户状态失败");
        }

        LogOperation("切换用户状态", new { }, id);
        return Success(user!, "用户状态已切换");
    }

    /// <summary>
    /// 批量删除用户
    /// </summary>
    [HttpPost("batch-delete")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public virtual async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
        {
            return ValidationFail("请至少选择一个用户");
        }

        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var result = await _userService.BatchDeleteUsersAsync(dto.Ids, currentUserId, isAdmin);

        LogOperation("批量删除用户", new { Ids = dto.Ids, Result = result.Message }, null);
        return Success(result, result.Message);
    }
}
