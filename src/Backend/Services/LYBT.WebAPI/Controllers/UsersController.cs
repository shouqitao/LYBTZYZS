using Asp.Versioning;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 用户管理控制器，提供RESTful API接口
/// 实现软删除策略：用户只能禁用/启用，不提供删除接口
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize] // 全部接口必须登录
public class UsersController : BaseController {
    private readonly IUserService _userService;

    public UsersController(IUserService userService, IMemoryCache cache, ILogger<UsersController> logger) 
        : base(logger, cache) {
        _userService = userService;
    }

    // 移除重复的分页查询接口，统一使用RESTful GET接口
    // 移除重复的新增用户接口，统一使用RESTful POST接口
    // 移除重复的编辑用户接口，统一使用RESTful PUT接口

    // 移除单独的Enable/Disable接口，统一使用ToggleStatus或Status接口

    /// <summary>
    /// 切换用户状态（启用/禁用）
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id) {
        var (operatorId, operatorName, operatorRole) = GetOperator();
        
        // 先获取用户当前状态
        var user = await _userService.GetByIdAsync(id);
        if (user == null) {
            return NotFound(new ProblemDetails {
                Title = "资源未找到"
            });
        }

        // 根据当前状态切换
        bool result;
        string message;
        if (user.Status == CommonStatus.Enabled) {
            result = await _userService.DisableAsync(id, operatorId, operatorName);
            message = "用户已禁用";
        } else {
            result = await _userService.EnableAsync(id, operatorId, operatorName);
            message = "用户已启用";
        }
        
        if (result) {
            LogOperation(message, null, id);
            return Ok(new { message });
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "状态切换失败",
                Status = 400
            });
        }
    }

    /// <summary>
    /// 批量禁用用户
    /// </summary>
    [HttpPatch("batch-disable")]
    public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        var count = await _userService.BatchDisableAsync(dto.Ids, operatorId, operatorName);
        LogOperation("批量禁用用户成功", new { Count = count, Ids = dto.Ids }, null);
        return Ok(new { message = $"成功禁用 {count} 个用户", count });
    }

    /// <summary>
    /// 批量启用用户
    /// </summary>
    [HttpPatch("batch-enable")]
    public async Task<IActionResult> BatchEnable([FromBody] BatchIdsDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        var count = await _userService.BatchEnableAsync(dto.Ids, operatorId, operatorName);
        LogOperation("批量启用用户成功", new { Count = count, Ids = dto.Ids }, null);
        return Ok(new { message = $"成功启用 {count} 个用户", count });
    }

    /// <summary>
    /// 管理员重置密码，恢复为默认值
    /// </summary>
    [HttpPost("resetPassword/{id}")]
    public async Task<IActionResult> ResetPassword(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        var result = await _userService.ResetPasswordAsync(id, operatorId, operatorName);
        if (result) {
            LogOperation("重置用户密码成功", null, id);
            return Ok(new { message = "密码重置成功" });
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "密码重置失败",
                Status = 400
            });
        }
    }

    /// <summary>
    /// 用户修改密码
    /// </summary>
    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto) {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized(new ProblemDetails {
                Title = "认证失败",
                Detail = "未登录或用户信息无效",
                Status = 401
            });

        var result = await _userService.ChangePasswordAsync(id, dto.OldPassword, dto.NewPassword);
        if (result) {
            LogOperation("修改密码成功", null, id);
            return Ok(new { message = "密码修改成功" });
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "密码修改失败",
                Status = 400
            });
        }
    }

    /// <summary>
    /// 用户修改个人信息
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> ChangeProfile([FromBody] ChangeProfileDto dto) {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized(new ProblemDetails {
                Title = "认证失败",
                Detail = "未登录或用户信息无效",
                Status = 401
            });

        var result = await _userService.ChangeProfileAsync(id, dto.RealName, dto.PhoneNumber);
        if (result) {
            LogOperation("修改个人信息成功", dto, id);
            return Ok(new { message = "个人信息修改成功" });
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "个人信息修改失败",
                Status = 400
            });
        }
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    [HttpGet("getRoles")]
    public ActionResult<IEnumerable<object>> GetRoles() {
        var roles = _userService.GetRoles();
        return Ok(roles);
    }

    // 移除重复的GetById接口，统一使用RESTful GET /{id}接口

    /// <summary>
    /// 获取启用的用户列表
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<LYBT.Shared.Models.Contracts.Users.UserDto>>> GetActiveUsers() {
        var users = await _userService.GetActiveUsersAsync();
        return Ok(users);
    }

    // ======================== RESTful 标准接口 ========================

    /// <summary>
    /// 获取所有用户列表 (RESTful GET /Users) - 支持模糊查询
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<LYBT.Shared.Models.Contracts.Users.UserDto>>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? username = null,
        [FromQuery] string? realName = null,
        [FromQuery] string? email = null,
        [FromQuery] string? phoneNumber = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null) {
        var (_, _, operatorRole) = GetOperator();
        var query = new LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto {
            CurrentPage = page,
            PageSize = pageSize,
            SearchKeyword = keyword,
            Username = username,
            RealName = realName,
            Email = email,
            PhoneNumber = phoneNumber,
            // Role = role ?? string.Empty, // Role字段已移除
            Status = isActive.HasValue ? (isActive.Value ? CommonStatus.Enabled : CommonStatus.Disabled) : (CommonStatus?)null
        };
        var result = await _userService.GetPagedAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// 根据ID获取用户 (RESTful GET /Users/{id})
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LYBT.Shared.Models.Contracts.Users.UserDto>> GetUser(Guid id) {
        var (_, _, operatorRole) = GetOperator();
        var user = await _userService.GetByIdAsync(id);
        if (user == null) {
            return NotFound(new ProblemDetails {
                Title = "资源未找到"
            });
        }
        return Ok(user);
    }

    /// <summary>
    /// 创建新用户 (RESTful POST /Users)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] LYBT.Shared.Models.Contracts.Users.UserCreateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        var result = await _userService.AddAsync(dto, operatorId, operatorName);
        if (result != null) {
            LogOperation("创建用户成功", result, result.Id);
            return Ok(result);
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "用户创建失败",
                Status = 400
            });
        }
    }

    /// <summary>
    /// 更新用户信息 (RESTful PUT /Users/{id})
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] LYBT.Shared.Models.Contracts.Users.UserUpdateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        // 确保DTO的ID与路由参数一致
        dto.Id = id;
        var result = await _userService.UpdateAsync(dto, operatorId, operatorName);
        if (result) {
            // 获取更新后的资源
                var (_, _, operatorRole) = GetOperator();
                var updated = await _userService.GetByIdAsync(dto.Id);
                LogOperation("用户信息更新成功", updated, dto.Id);
                return Ok(updated);
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "用户信息更新失败",
                Status = 400
            });
        }
    }

    // 注意：本系统采用软删除策略，不提供DELETE接口
    // 请使用 PATCH /Users/{id}/toggle-status 来切换用户状态
}