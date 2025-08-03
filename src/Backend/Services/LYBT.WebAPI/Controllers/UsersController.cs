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
            var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.Staff;
            return (opId, userName, role);
        }
        throw new UnauthorizedAccessException("未登录或用户信息无效");
    }

    /// <summary>
    /// 分页查找用户（关键词、角色、状态筛选）
    /// 权限控制：禁用的用户仅管理员可查询
    /// </summary>
    [HttpPost("paged")]
    public async Task<IActionResult> GetPaged([FromBody] LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto query) {
        var (_, _, operatorRole) = GetOperator();
        var result = await _userService.GetPagedAsync(query, operatorRole);
        return Ok(ApiResponse<PaginatedResult<LYBT.Shared.Models.Contracts.Users.UserDto>>.Success(result));
    }

    /// <summary>
    /// 新增用户，密码将设为配置的默认值
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] LYBT.Shared.Models.Contracts.Users.UserCreateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.AddAsync(dto, operatorId, operatorName);
            if (result) {
                return Ok(ApiResponse<object>.Success("用户创建成功"));
            } else {
                return BadRequest(ApiResponse<object>.Fail("用户创建失败", 400));
            }
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 编辑用户
    /// </summary>
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] LYBT.Shared.Models.Contracts.Users.UserUpdateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.UpdateAsync(dto, operatorId, operatorName);
            if (result) {
                return Ok(ApiResponse<object>.Success("用户信息更新成功"));
            } else {
                return BadRequest(ApiResponse<object>.Fail("用户信息更新失败", 400));
            }
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 禁用用户（软删除）
    /// </summary>
    [HttpPatch("{id}/disable")]
    public async Task<IActionResult> Disable(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.DisableAsync(id, operatorId, operatorName);
            return result ? Ok(ApiResponse<object>.Success("用户已禁用")) : BadRequest(ApiResponse<object>.Fail("禁用用户失败", 400));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    [HttpPatch("{id}/enable")]
    public async Task<IActionResult> Enable(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.EnableAsync(id, operatorId, operatorName);
            return result ? Ok(ApiResponse<object>.Success("用户已启用")) : BadRequest(ApiResponse<object>.Fail("启用用户失败", 400));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 切换用户状态（启用/禁用）
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id) {
        var (operatorId, operatorName, operatorRole) = GetOperator();
        try {
            // 先获取用户当前状态
            var user = await _userService.GetByIdAsync(id, operatorRole);
            if (user == null) {
                return NotFound(ApiResponse<object>.Fail("用户不存在", 404));
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
            
            return result ? Ok(ApiResponse<object>.Success(message)) : BadRequest(ApiResponse<object>.Fail("状态切换失败", 400));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 批量禁用用户
    /// </summary>
    [HttpPatch("batch-disable")]
    public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var count = await _userService.BatchDisableAsync(dto.Ids, operatorId, operatorName);
            return Ok(ApiResponse<object>.Success($"成功禁用 {count} 个用户"));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 批量启用用户
    /// </summary>
    [HttpPatch("batch-enable")]
    public async Task<IActionResult> BatchEnable([FromBody] BatchIdsDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var count = await _userService.BatchEnableAsync(dto.Ids, operatorId, operatorName);
            return Ok(ApiResponse<object>.Success($"成功启用 {count} 个用户"));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 管理员重置密码，恢复为默认值
    /// </summary>
    [HttpPost("resetPassword/{id}")]
    public async Task<IActionResult> ResetPassword(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.ResetPasswordAsync(id, operatorId, operatorName);
            return result ? Ok(ApiResponse<object>.Success("密码重置成功")) : BadRequest(ApiResponse<object>.Fail("密码重置失败", 400));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 用户修改密码
    /// </summary>
    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto) {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized(new { success = false, message = "未登录或用户信息无效" });

        try {
            var result = await _userService.ChangePasswordAsync(id, dto.OldPassword, dto.NewPassword);
            return result ? Ok(ApiResponse<object>.Success("密码修改成功")) : BadRequest(ApiResponse<object>.Fail("密码修改失败", 400));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 用户修改个人信息
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> ChangeProfile([FromBody] ChangeProfileDto dto) {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized(new { success = false, message = "未登录或用户信息无效" });

        try {
            var result = await _userService.ChangeProfileAsync(id, dto.RealName, dto.Email, dto.PhoneNumber);
            return result ? Ok(ApiResponse<object>.Success("个人信息修改成功")) : BadRequest(ApiResponse<object>.Fail("个人信息修改失败", 400));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
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

    // ======================== RESTful 标准接口 ========================

    /// <summary>
    /// 获取所有用户列表 (RESTful GET /Users) - 支持模糊查询
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
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
        return Ok(ApiResponse<PaginatedResult<LYBT.Shared.Models.Contracts.Users.UserDto>>.Success(result));
    }

    /// <summary>
    /// 根据ID获取用户 (RESTful GET /Users/{id})
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id) {
        var (_, _, operatorRole) = GetOperator();
        var user = await _userService.GetByIdAsync(id, operatorRole);
        if (user == null) {
            return NotFound(ApiResponse<object>.Fail("用户不存在", 404));
        }
        return Ok(ApiResponse<object>.Success(user));
    }

    /// <summary>
    /// 创建新用户 (RESTful POST /Users)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] LYBT.Shared.Models.Contracts.Users.UserCreateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.AddAsync(dto, operatorId, operatorName);
            if (result) {
                return Ok(ApiResponse<object>.Success("用户创建成功"));
            } else {
                return BadRequest(ApiResponse<object>.Fail("用户创建失败", 400));
            }
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 更新用户信息 (RESTful PUT /Users/{id})
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] LYBT.Shared.Models.Contracts.Users.UserUpdateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            // 确保DTO的ID与路由参数一致
            dto.Id = id;
            var result = await _userService.UpdateAsync(dto, operatorId, operatorName);
            if (result) {
                return Ok(ApiResponse<object>.Success("用户信息更新成功"));
            } else {
                return BadRequest(ApiResponse<object>.Fail("用户信息更新失败", 400));
            }
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    // 注意：本系统采用软删除策略，不提供DELETE接口
    // 请使用 PATCH /Users/{id}/disable 来禁用用户
    // 请使用 PATCH /Users/{id}/enable 来启用用户
}