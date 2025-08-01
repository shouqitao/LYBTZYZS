using Asp.Versioning;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.Models.Users;
using LYBT.Module.Users.Interfaces;
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
    /// 映射本地DTO到共享DTO
    /// </summary>
    private LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto MapToSharedQuery(UserQueryDto localDto) {
        return new LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto {
            CurrentPage = localDto.Page,
            PageSize = localDto.PageSize,
            Username = localDto.Keyword,
            RealName = localDto.Keyword,
            Role = localDto.Role,
            IsActive = localDto.IsActive
        };
    }

    /// <summary>
    /// 映射本地创建DTO到共享DTO
    /// </summary>
    private LYBT.Shared.Models.Contracts.Users.UserCreateDto MapToSharedCreateDto(UserCreateDto localDto) {
        return new LYBT.Shared.Models.Contracts.Users.UserCreateDto {
            Username = localDto.UserName,
            RealName = localDto.RealName,
            Role = localDto.Role,
            Email = localDto.Email,
            PhoneNumber = localDto.PhoneNumber,
            IsActive = localDto.IsActive
        };
    }

    /// <summary>
    /// 映射本地更新DTO到共享DTO
    /// </summary>
    private LYBT.Shared.Models.Contracts.Users.UserUpdateDto MapToSharedUpdateDto(UserDetailDto localDto) {
        return new LYBT.Shared.Models.Contracts.Users.UserUpdateDto {
            Id = localDto.Id,
            Username = "user_" + localDto.Id.ToString("N")[..8], // 生成默认用户名
            RealName = localDto.RealName,
            Role = localDto.Role,
            Email = localDto.Email,
            PhoneNumber = localDto.PhoneNumber,
            IsActive = localDto.IsActive
        };
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
    public async Task<IActionResult> Add([FromBody] UserCreateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var sharedDto = MapToSharedCreateDto(dto);
            var result = await _userService.AddAsync(sharedDto, operatorId, operatorName);
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
    public async Task<IActionResult> Update([FromBody] UserDetailDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var sharedDto = MapToSharedUpdateDto(dto);
            var result = await _userService.UpdateAsync(sharedDto, operatorId, operatorName);
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
    [HttpPost("disable/{id}")]
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
    [HttpPost("enable/{id}")]
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
    /// 批量禁用用户
    /// </summary>
    [HttpPost("batchDisable")]
    public async Task<IActionResult> BatchDisable([FromBody] UserBatchIdsDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var count = await _userService.BatchDisableAsync(dto.UserIds, operatorId, operatorName);
            return Ok(ApiResponse<object>.Success($"成功禁用 {count} 个用户"));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    /// <summary>
    /// 批量启用用户
    /// </summary>
    [HttpPost("batchEnable")]
    public async Task<IActionResult> BatchEnable([FromBody] UserBatchIdsDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var count = await _userService.BatchEnableAsync(dto.UserIds, operatorId, operatorName);
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
    [HttpPost("changePassword")]
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
    [HttpPost("changeProfile")]
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
    /// 获取所有用户列表 (RESTful GET /Users)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        var (_, _, operatorRole) = GetOperator();
        var query = new UserQueryDto { 
            Page = page, 
            PageSize = pageSize 
        };
        var sharedQuery = MapToSharedQuery(query);
        var result = await _userService.GetPagedAsync(sharedQuery, operatorRole);
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
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var sharedDto = MapToSharedCreateDto(dto);
            var result = await _userService.AddAsync(sharedDto, operatorId, operatorName);
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
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserDetailDto dto) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            // 确保DTO的ID与路由参数一致
            dto.Id = id;
            var sharedDto = MapToSharedUpdateDto(dto);
            var result = await _userService.UpdateAsync(sharedDto, operatorId, operatorName);
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
    /// 删除用户 (RESTful DELETE /Users/{id}) - 实际执行禁用操作
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id) {
        var (operatorId, operatorName, _) = GetOperator();
        try {
            var result = await _userService.DisableAsync(id, operatorId, operatorName);
            return result ? Ok(ApiResponse<object>.Success("用户已删除(禁用)")) : BadRequest(ApiResponse<object>.Fail("删除用户失败", 400));
        } catch (Exception ex) {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    // 注意：不提供真正的删除接口，用户只能禁用，不能删除
    // 原有的删除相关接口已移除，改为禁用/启用操作
}