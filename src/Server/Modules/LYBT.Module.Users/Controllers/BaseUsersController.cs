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
/// 继承 BaseCrudController 提供标准 CRUD，保留用户特化方法
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseUsersController : BaseCrudController
{
    protected BaseUsersController(ISender sender, ILogger logger)
        : base(sender, logger)
    {
    }

    #region 重写 CRUD 方法（添加授权策略）

    [HttpGet]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public override async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;

        var result = await Sender.Send(new GetUsersQuery(page, pageSize, keyword), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value, "查询成功");
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var result = await Sender.Send(new GetUserQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "用户不存在");
        return Success(result.Value, "查询成功");
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public override async Task<IActionResult> Create([FromBody] object dto, CancellationToken ct)
    {
        if (dto is not UserInputDto inputDto)
            return ValidationFail("无效的请求数据");

        var (operatorId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;
        var result = await Sender.Send(new CreateUserCommand(inputDto, operatorId, isAdmin), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建失败");
        LogOperation("创建用户成功", result.Value, null);
        return Success(result.Value, "用户创建成功");
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public override async Task<IActionResult> Update(Guid id, [FromBody] object dto, CancellationToken ct)
    {
        if (ValidateGuid(id, "用户ID") is { } guidError) return guidError;
        if (dto is not UserInputDto inputDto)
            return ValidationFail("无效的请求数据");

        var (operatorId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;
        var result = await Sender.Send(new UpdateUserCommand(id, inputDto, operatorId, isAdmin), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "更新失败");
        }
        LogOperation("更新用户成功", result.Value, id);
        return Success(result.Value, "用户更新成功");
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (operatorId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;
        var result = await Sender.Send(new DeleteUserCommand(id, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "删除失败");
        }
        LogOperation("删除用户成功", null, id);
        return Success("删除成功");
    }

    [HttpPost("{id:guid}/toggle-status")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
        var result = await Sender.Send(new ToggleUserStatusCommand(id, operatorId, isAdmin), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");

        LogOperation("切换用户状态", null, id);
        return Success(result.Value, "状态已切换");
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "用户ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new RestoreUserCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("未被删除") == true)
                return BusinessFail(result.Error);
            return NotFound(result.Error ?? "用户不存在");
        }

        LogOperation("恢复用户", null, id);
        return Success(result.Value, "用户恢复成功");
    }

    [HttpPost("batch-delete")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("请至少选择一个用户");

        var (operatorId, _, currentRole) = GetOperator();
        var isAdmin = currentRole == UserRole.SuperAdmin || currentRole == UserRole.Admin;
        var result = await Sender.Send(new BatchDeleteUsersCommand(dto.Ids, operatorId, isAdmin), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");

        LogOperation("批量删除用户", new { Ids = dto.Ids, Result = result.Value.Message }, null);
        return Success(result.Value, result.Value.Message);
    }

    #endregion

    #region 特化方法

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
    [ProducesResponseType(401)]
    public virtual async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userId = BaseClaimsHelper.GetCurrentUserId(User);
        var result = await Sender.Send(new GetCurrentUserQuery(userId), cancellationToken);
        if (!result.IsSuccess) return NotFound(result.Error ?? "用户不存在");
        return Success(result.Value!);
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

        var result = await Sender.Send(new ResetPasswordCommand(id), ct);

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

        var result = await Sender.Send(new ChangeProfileCommand(id, dto, currentUserId), ct);

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

        var result = await Sender.Send(new ChangePasswordCommand(id, request.OldPassword, request.NewPassword, currentUserId), ct);

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

        var result = await Sender.Send(new BatchEnableUsersCommand(dto.Ids), ct);

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

        var result = await Sender.Send(new BatchDisableUsersCommand(dto.Ids), ct);

        LogOperation("批量禁用用户", new { Ids = dto.Ids, Result = result.Value?.Message }, null);
        return Success(result.Value!, result.Value?.Message ?? "批量禁用完成");
    }

    #endregion
}
