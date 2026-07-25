using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Application.Commands;
using LYBT.Module.Users.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Controllers;

/// <summary>
/// 用户管理 Controller 共享基类
/// 供 WebAPI 和 LocalWebAPI 共享，减少重复代码
/// </summary>

[ApiController]
[Authorize]
public abstract class BaseUsersController : BaseApiController
{
    private readonly ISender _sender;

    protected BaseUsersController(ISender sender, ILogger logger)
        : base(logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
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

        var result = await _sender.Send(new GetUsersQuery(page, pageSize, keyword, role, status), cancellationToken);
        if (!result.IsSuccess) return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value!, "查询成功");
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
        var result = await _sender.Send(new GetCurrentUserQuery(userId), cancellationToken);
        if (!result.IsSuccess) return NotFound(result.Error ?? "用户不存在");
        return Success(result.Value!);
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

        var result = await _sender.Send(new GetUserQuery(id), cancellationToken);
        if (!result.IsSuccess) return NotFound(result.Error ?? "用户不存在");
        return Success(result.Value!);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("ApiCalls")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 201)]
    [ProducesResponseType(400)]
    public virtual async Task<IActionResult> Create([FromBody] UserInputDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var result = await _sender.Send(new CreateUserCommand(dto, currentUserId, isAdmin), cancellationToken);

        if (!result.IsSuccess)
        {
            return BusinessFail(result.Error ?? "创建用户失败");
        }

        LogOperation("创建用户", dto, result.Value!.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.Id, version = ApiVersionConstants.V1 },
            ApiResponse<UserDetailDto>.CreateSuccess(result.Value, "创建成功"));
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{id:guid}")]
    [EnableRateLimiting("ApiCalls")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] UserInputDto dto, CancellationToken ct = default)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var result = await _sender.Send(new UpdateUserCommand(id, dto, currentUserId, isAdmin), ct);

        if (!result.IsSuccess)
        {
            if (result.Error == "用户不存在") return NotFound(result.Error);
            if (result.Error?.StartsWith("您没有权限") == true) return Forbid(result.Error);
            if (result.Error?.StartsWith("系统管理员") == true) return Forbid(result.Error);
            return BusinessFail(result.Error ?? "更新用户失败");
        }

        LogOperation("更新用户", dto, id);
        return Success(result.Value!, "用户更新成功");
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("ApiCalls")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var result = await _sender.Send(new DeleteUserCommand(id, currentUserId, isAdmin), ct);

        if (!result.IsSuccess)
        {
            if (result.Error == "用户不存在") return NotFound(result.Error);
            if (result.Error?.StartsWith("不能删除") == true) return Forbid(result.Error);
            if (result.Error?.StartsWith("系统管理员") == true) return Forbid(result.Error);
            if (result.Error?.StartsWith("您没有权限") == true) return Forbid(result.Error);
            return BusinessFail(result.Error ?? "删除用户失败");
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
    public virtual async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequestDto request, CancellationToken ct = default)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var result = await _sender.Send(new ResetPasswordCommand(id), ct);

        if (!result.IsSuccess)
        {
            if (result.Error == "用户不存在") return NotFound(result.Error);
            return BusinessFail(result.Error ?? "密码重置失败");
        }

        LogOperation("重置用户密码", new { AutoGenerated = true }, id);
        return Success(new ResetPasswordResponseDto
        {
            Success = true,
            TemporaryPassword = result.Value!.TemporaryPassword
        }, "密码重置成功");
    }

    /// <summary>
    /// 修改个人资料
    /// </summary>
    [HttpPut("{id:guid}/profile")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> ChangeProfile(Guid id, [FromBody] ChangeProfileDto dto, CancellationToken ct = default)
    {
        var (currentUserId, _, _) = GetOperator();

        var result = await _sender.Send(new ChangeProfileCommand(id, dto, currentUserId), ct);

        if (!result.IsSuccess)
        {
            if (result.Error?.StartsWith("只能修改") == true) return Forbid(result.Error);
            if (result.Error == "用户不存在") return NotFound(result.Error);
            return BusinessFail(result.Error ?? "个人资料修改失败");
        }

        LogOperation("修改个人资料", new { RealName = dto.RealName, PhoneNumber = dto.PhoneNumber }, id);
        return Success(result.Value!, "个人资料修改成功");
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPut("{id:guid}/change-password")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public virtual async Task<IActionResult> ChangePassword(Guid id, [FromBody] LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request, CancellationToken ct = default)
    {
        var (currentUserId, _, _) = GetOperator();

        var result = await _sender.Send(new ChangePasswordCommand(id, request.OldPassword, request.NewPassword, currentUserId), ct);

        if (!result.IsSuccess)
        {
            if (result.Error?.StartsWith("只能修改") == true) return Forbid(result.Error);
            if (result.Error == "用户不存在") return NotFound(result.Error);
            return BusinessFail(result.Error ?? "密码修改失败");
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
    public virtual async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct = default)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var result = await _sender.Send(new ToggleUserStatusCommand(id, currentUserId, isAdmin), ct);

        if (!result.IsSuccess)
        {
            if (result.Error == "用户不存在") return NotFound(result.Error);
            if (result.Error?.StartsWith("系统管理员") == true) return Forbid(result.Error);
            if (result.Error?.StartsWith("您没有权限") == true) return Forbid(result.Error);
            return BusinessFail(result.Error ?? "切换用户状态失败");
        }

        LogOperation("切换用户状态", new { }, id);
        return Success(result.Value!, "用户状态已切换");
    }

    /// <summary>
    /// 批量删除用户
    /// </summary>
    [HttpPost("batch-delete")]
    [EnableRateLimiting("ApiCalls")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public virtual async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
        {
            return ValidationFail("请至少选择一个用户");
        }

        var (currentUserId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;

        var result = await _sender.Send(new BatchDeleteUsersCommand(dto.Ids, currentUserId, isAdmin), ct);

        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");

        LogOperation("批量删除用户", new { Ids = dto.Ids, Result = result.Value.Message }, null);
        return Success(result.Value, result.Value.Message ?? "批量删除完成");
    }

    /// <summary>
    /// 恢复已删除用户
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public virtual async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();

        var result = await _sender.Send(new RestoreUserCommand(id, operatorId), ct);

        if (!result.IsSuccess)
        {
            if (result.Error == "用户不存在") return NotFound(result.Error);
            if (result.Error?.StartsWith("该用户未被删除") == true) return BusinessFail(result.Error);
            return BusinessFail(result.Error ?? "恢复用户失败");
        }

        LogOperation("恢复用户", new { }, id);
        return Success(result.Value!, "用户恢复成功");
    }

    /// <summary>
    /// 批量启用用户
    /// </summary>
    [HttpPost("batch-enable")]
    [EnableRateLimiting("ApiCalls")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public virtual async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
        {
            return ValidationFail("请至少选择一个用户");
        }

        var result = await _sender.Send(new BatchEnableUsersCommand(dto.Ids), ct);

        LogOperation("批量启用用户", new { Ids = dto.Ids, Result = result.Value?.Message }, null);
        return Success(result.Value!, result.Value?.Message ?? "批量启用完成");
    }

    /// <summary>
    /// 批量禁用用户
    /// </summary>
    [HttpPost("batch-disable")]
    [EnableRateLimiting("ApiCalls")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public virtual async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
        {
            return ValidationFail("请至少选择一个用户");
        }

        var result = await _sender.Send(new BatchDisableUsersCommand(dto.Ids), ct);

        LogOperation("批量禁用用户", new { Ids = dto.Ids, Result = result.Value?.Message }, null);
        return Success(result.Value!, result.Value?.Message ?? "批量禁用完成");
    }
}


