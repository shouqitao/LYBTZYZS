using System.Security.Claims;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService, ILogger<UsersController> logger) : base(logger)
    {
        _userService = userService;
    }

    private Guid GetCurrentUserId()
        => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private UserRole GetCurrentUserRole()
        => Enum.TryParse<UserRole>(User.FindFirst(ClaimTypes.Role)?.Value, out var role) ? role : UserRole.Receptionist;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? keyword = null)
    {
        var result = await _userService.SearchAsync(keyword ?? "");
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetByIdAsync(userId);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserInputDto dto)
    {
        var result = await _userService.CreateAsync(dto, GetCurrentUserRole());
        return HandleResult(result, "用户创建成功");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserInputDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto, GetCurrentUserRole());
        return HandleResult(result, "用户更新成功");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _userService.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        return HandleResult(result, "用户删除成功");
    }

    [HttpPut("{id:guid}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        if (id != GetCurrentUserId())
            return Forbid("只能修改自己的密码");
        var result = await _userService.ChangePasswordAsync(id, request.OldPassword, request.NewPassword);
        return HandleResult(result, "密码修改成功");
    }

    [HttpPut("{id:guid}/profile")]
    public async Task<IActionResult> ChangeProfile(Guid id, [FromBody] ChangeProfileDto dto)
    {
        if (id != GetCurrentUserId())
            return Forbid("只能修改自己的个人资料");
        var result = await _userService.ChangeProfileAsync(id, dto);
        return HandleResult(result, "个人资料修改成功");
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _userService.ToggleStatusAsync(id, GetCurrentUserRole());
        return HandleResult(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var result = await _userService.RestoreAsync(id, GetCurrentUserRole());
        return HandleResult(result);
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var result = await _userService.BatchDeleteAsync(request.Ids, GetCurrentUserId(), GetCurrentUserRole());
        return HandleResult(result);
    }

    [HttpPost("batch-enable")]
    public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var result = await _userService.BatchUpdateStatusAsync(request.Ids, CommonStatus.Enabled, GetCurrentUserId(), GetCurrentUserRole());
        return HandleResult(result);
    }

    [HttpPost("batch-disable")]
    public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var result = await _userService.BatchUpdateStatusAsync(request.Ids, CommonStatus.Disabled, GetCurrentUserId(), GetCurrentUserRole());
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequestDto request)
    {
        var result = await _userService.ResetPasswordAsync(id, request);
        return HandleResult(result, "密码重置成功");
    }
}
