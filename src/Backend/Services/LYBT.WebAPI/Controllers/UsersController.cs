using Asp.Versioning;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
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
            var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.RegistrationStaff;
            return (opId, userName, role);
        }
        throw new UnauthorizedAccessException("未登录或用户信息无效");
    }

    /// <summary>
    /// 分页查找用户（关键词、角色、状态筛选）
    /// 权限控制：禁用的用户仅管理员可查询
    /// </summary>
    [HttpPost("paged")]
    public async Task<ActionResult<PaginatedResult<LYBT.Shared.Models.Contracts.Users.UserDto>>> GetPaged([FromBody] LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto query) {
        var (_, _, operatorRole) = GetOperator();
        var result = await _userService.GetPagedAsync(query, operatorRole);
        return Ok(result);
    }

    /// <summary>
    /// 新增用户，密码将设为配置的默认值
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] LYBT.Shared.Models.Contracts.Users.UserCreateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        var result = await _userService.AddAsync(dto, operatorId, operatorName);
        if (result) {
            return Ok(new { message = "用户创建成功" });
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "用户创建失败",
                Status = 400
            });
        }
    }

    /// <summary>
    /// 编辑用户
    /// </summary>
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] LYBT.Shared.Models.Contracts.Users.UserUpdateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        var result = await _userService.UpdateAsync(dto, operatorId, operatorName);
        if (result) {
            return Ok(new { message = "用户信息更新成功" });
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "用户信息更新失败",
                Status = 400
            });
        }
    }

    /// <summary>
    /// 禁用用户（软删除）
    /// </summary>
    [HttpPatch("{id}/disable")]
    public async Task<IActionResult> Disable(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        var result = await _userService.DisableAsync(id, operatorId, operatorName);
        if (result) {
            return Ok(new { message = "用户已禁用" });
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "禁用用户失败",
                Status = 400
            });
        }
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    [HttpPatch("{id}/enable")]
    public async Task<IActionResult> Enable(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        var result = await _userService.EnableAsync(id, operatorId, operatorName);
        if (result) {
            return Ok(new { message = "用户已启用" });
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "启用用户失败",
                Status = 400
            });
        }
    }

    /// <summary>
    /// 切换用户状态（启用/禁用）
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id) {
        var (operatorId, operatorName, operatorRole) = GetOperator();
        
        // 先获取用户当前状态
        var user = await _userService.GetByIdAsync(id, operatorRole);
        if (user == null) {
            return NotFound(new ProblemDetails {
                Title = "资源未找到",
                Detail = "用户不存在",
                Status = 404
            });
        }

        // 根据当前状态切换
        bool result;
        string message;
        if (user.IsActive) {
            result = await _userService.DisableAsync(id, operatorId, operatorName);
            message = "用户已禁用";
        } else {
            result = await _userService.EnableAsync(id, operatorId, operatorName);
            message = "用户已启用";
        }
        
        if (result) {
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
        return Ok(new { message = $"成功禁用 {count} 个用户", count });
    }

    /// <summary>
    /// 批量启用用户
    /// </summary>
    [HttpPatch("batch-enable")]
    public async Task<IActionResult> BatchEnable([FromBody] BatchIdsDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        var count = await _userService.BatchEnableAsync(dto.Ids, operatorId, operatorName);
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

        var result = await _userService.ChangeProfileAsync(id, dto.RealName, dto.Email, dto.PhoneNumber);
        if (result) {
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

    /// <summary>
    /// 根据Id获取用户详情
    /// 权限控制：禁用的用户仅管理员可查询
    /// </summary>
    [HttpGet("getById/{id}")]
    public async Task<ActionResult<LYBT.Shared.Models.Contracts.Users.UserDto>> GetById(Guid id) {
        var (_, _, operatorRole) = GetOperator();
        var user = await _userService.GetByIdAsync(id, operatorRole);
        if (user == null) {
            return NotFound(new ProblemDetails {
                Title = "资源未找到",
                Detail = "用户不存在",
                Status = 404
            });
        }
        return Ok(user);
    }

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
        [FromQuery] UserRole? role = null,
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
            Role = role,
            IsActive = isActive
        };
        var result = await _userService.GetPagedAsync(query, operatorRole);
        return Ok(result);
    }

    /// <summary>
    /// 根据ID获取用户 (RESTful GET /Users/{id})
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LYBT.Shared.Models.Contracts.Users.UserDto>> GetUser(Guid id) {
        var (_, _, operatorRole) = GetOperator();
        var user = await _userService.GetByIdAsync(id, operatorRole);
        if (user == null) {
            return NotFound(new ProblemDetails {
                Title = "资源未找到",
                Detail = "用户不存在",
                Status = 404
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
        if (result) {
            return Ok(new { message = "用户创建成功" });
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
            return Ok(new { message = "用户信息更新成功" });
        } else {
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "用户信息更新失败",
                Status = 400
            });
        }
    }

    // 注意：本系统采用软删除策略，不提供DELETE接口
    // 请使用 PATCH /Users/{id}/disable 来禁用用户
    // 请使用 PATCH /Users/{id}/enable 来启用用户
}