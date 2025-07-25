using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

/// <summary>
/// 用户管理控制器，提供RESTful API接口
/// 实现软删除策略：用户只能禁用/启用，不提供删除接口
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize] // 全部接口必须登录
public class UsersController : ControllerBase {
    private readonly IUserService _userService;

    public UsersController(IUserService userService) {
        _userService = userService;
    }

    /// <summary>
    /// 获取当前操作者信息
    /// </summary>
    private (Guid operatorId, string operatorName, UserRole operatorRole) GetOperator() {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User?.Identity?.Name;
        var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value;

        if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName)) {
            var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.Staff;
            return (opId, userName, role);
        }
        throw new UnauthorizedAccessException("未登录或用户信息无效");
    }

    /// <summary>
    /// 分页查找用户（关键词、角色、状态筛选）
    /// 权限控制：禁用的用户仅管理员可查询
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] UserQueryDto query) {
        var (_, _, operatorRole) = GetOperator();
        var (users, total) = await _userService.SearchAsync(query, operatorRole);
        return Ok(new { total, users });
    }

    /// <summary>
    /// 新增用户，密码将设为配置的默认值
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] UserCreateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.AddAsync(dto, operatorId, operatorName);
            return result ? Ok(new { success = true, message = "用户创建成功" }) : BadRequest(new { success = false, message = "用户创建失败" });
        } catch (Exception ex) {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 编辑用户
    /// </summary>
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UserDetailDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.UpdateAsync(dto, operatorId, operatorName);
            return result ? Ok(new { success = true, message = "用户信息更新成功" }) : BadRequest(new { success = false, message = "用户信息更新失败" });
        } catch (Exception ex) {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 禁用用户（软删除）
    /// </summary>
    [HttpPost("disable/{id}")]
    public async Task<IActionResult> Disable(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.DisableAsync(id, operatorId, operatorName);
            return result ? Ok(new { success = true, message = "用户已禁用" }) : BadRequest(new { success = false, message = "禁用用户失败" });
        } catch (Exception ex) {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    [HttpPost("enable/{id}")]
    public async Task<IActionResult> Enable(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.EnableAsync(id, operatorId, operatorName);
            return result ? Ok(new { success = true, message = "用户已启用" }) : BadRequest(new { success = false, message = "启用用户失败" });
        } catch (Exception ex) {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 批量禁用用户
    /// </summary>
    [HttpPost("batchDisable")]
    public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        var count = await _userService.BatchDisableAsync(dto.Ids, operatorId, operatorName);
        return Ok(new { success = true, count, message = $"成功禁用 {count} 个用户" });
    }

    /// <summary>
    /// 批量启用用户
    /// </summary>
    [HttpPost("batchEnable")]
    public async Task<IActionResult> BatchEnable([FromBody] BatchIdsDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        var count = await _userService.BatchEnableAsync(dto.Ids, operatorId, operatorName);
        return Ok(new { success = true, count, message = $"成功启用 {count} 个用户" });
    }

    /// <summary>
    /// 管理员重置密码，恢复为默认值
    /// </summary>
    [HttpPost("resetPassword/{id}")]
    public async Task<IActionResult> ResetPassword(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.ResetPasswordAsync(id, operatorId, operatorName);
            return result ? Ok(new { success = true, message = "密码重置成功" }) : BadRequest(new { success = false, message = "密码重置失败" });
        } catch (Exception ex) {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 用户修改密码
    /// </summary>
    [HttpPost("changePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto) {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized(new { success = false, message = "未登录或用户信息无效" });

        try {
            var result = await _userService.ChangePasswordAsync(id, dto.OldPassword, dto.NewPassword);
            return result ? Ok(new { success = true, message = "密码修改成功" }) : BadRequest(new { success = false, message = "密码修改失败" });
        } catch (Exception ex) {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 用户修改个人信息
    /// </summary>
    [HttpPost("changeProfile")]
    public async Task<IActionResult> ChangeProfile([FromBody] ChangeProfileDto dto) {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized(new { success = false, message = "未登录或用户信息无效" });

        try {
            var result = await _userService.ChangeProfileAsync(id, dto.RealName, dto.Email, dto.PhoneNumber);
            return result ? Ok(new { success = true, message = "个人信息修改成功" }) : BadRequest(new { success = false, message = "个人信息修改失败" });
        } catch (Exception ex) {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    [HttpGet("getRoles")]
    public IActionResult GetRoles() {
        var roles = _userService.GetRoles();
        return Ok(roles);
    }

    /// <summary>
    /// 根据Id获取用户详情
    /// 权限控制：禁用的用户仅管理员可查询
    /// </summary>
    [HttpGet("getById/{id}")]
    public async Task<IActionResult> GetById(Guid id) {
        var (_, _, operatorRole) = GetOperator();
        var user = await _userService.GetByIdAsync(id, operatorRole);
        return user == null ? NotFound(new { success = false, message = "用户不存在" }) : Ok(user);
    }

    /// <summary>
    /// 获取启用的用户列表
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveUsers() {
        var users = await _userService.GetActiveUsersAsync();
        return Ok(users);
    }

    // 注意：不提供删除接口，用户只能禁用，不能删除
    // 原有的删除相关接口已移除，改为禁用/启用操作
}