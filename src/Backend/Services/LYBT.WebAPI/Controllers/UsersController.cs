using Asp.Versioning;
using LYBT.Infrastructure.Web;
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
/// 用户管理控制器 - 统一API响应格式和错误处理
/// 实现软删除策略：用户只能禁用/启用，不提供删除接口
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize] // 全部接口必须登录
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService, IMemoryCache cache, ILogger<UsersController> logger)
        : base(logger, cache)
    {
        _userService = userService;
    }

    // 移除重复的分页查询接口，统一使用RESTful GET接口
    // 移除重复的新增用户接口，统一使用RESTful POST接口
    // 移除重复的编辑用户接口，统一使用RESTful PUT接口

    // 移除单独的Enable/Disable接口，统一使用ToggleStatus或Status接口

    /// <summary>
    /// 切换用户状态（启用/禁用） - 统一API响应格式
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    public async Task<ActionResult<ApiResponse>> ToggleStatus(Guid id)
    {
        try
        {
            var validation = ValidateGuid(id, "用户ID");
            if (validation != null) return validation;

            var (operatorId, operatorName, operatorRole) = GetOperator();

            // 先获取用户当前状态
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound("用户不存在", ApiErrorCodes.USER_NOT_FOUND);
            }

            // 根据当前状态切换
            bool result;
            string message;
            if (user.Status == CommonStatus.Enabled)
            {
                result = await _userService.DisableAsync(id, operatorId, operatorName);
                message = "用户已禁用";
            }
            else
            {
                result = await _userService.EnableAsync(id, operatorId, operatorName);
                message = "用户已启用";
            }

            if (!result)
            {
                return BusinessFail("状态切换失败", ApiErrorCodes.DATA_UPDATE_FAILED);
            }

            LogOperation(message, null, id);
            return Success(message);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "切换用户状态", id);
        }
    }

    /// <summary>
    /// 批量禁用用户 - 统一API响应格式
    /// </summary>
    [HttpPatch("batch-disable")]
    public async Task<ActionResult<ApiResponse>> BatchDisable([FromBody] BatchIdsDto dto)
    {
        try
        {
            var validation = ValidateModel();
            if (validation != null) return validation;

            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("用户ID列表不能为空");
            }

            var (operatorId, operatorName, _) = GetOperator();
            var count = await _userService.BatchDisableAsync(dto.Ids, operatorId, operatorName);
            
            var message = $"成功禁用 {count} 个用户，共 {dto.Ids.Count} 个";
            LogOperation("批量禁用用户", new { count, total = dto.Ids.Count });
            return Success(message);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "批量禁用用户", dto);
        }
    }

    /// <summary>
    /// 批量启用用户 - 统一API响应格式
    /// </summary>
    [HttpPatch("batch-enable")]
    public async Task<ActionResult<ApiResponse>> BatchEnable([FromBody] BatchIdsDto dto)
    {
        try
        {
            var validation = ValidateModel();
            if (validation != null) return validation;

            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("用户ID列表不能为空");
            }

            var (operatorId, operatorName, _) = GetOperator();
            var count = await _userService.BatchEnableAsync(dto.Ids, operatorId, operatorName);
            
            var message = $"成功启用 {count} 个用户，共 {dto.Ids.Count} 个";
            LogOperation("批量启用用户", new { count, total = dto.Ids.Count });
            return Success(message);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "批量启用用户", dto);
        }
    }

    /// <summary>
    /// 管理员重置密码，恢复为默认值 - 统一API响应格式
    /// </summary>
    [HttpPost("resetPassword/{id}")]
    public async Task<ActionResult<ApiResponse>> ResetPassword(Guid id)
    {
        try
        {
            var validation = ValidateGuid(id, "用户ID");
            if (validation != null) return validation;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _userService.ResetPasswordAsync(id, operatorId, operatorName);
            
            if (!result)
            {
                return BusinessFail("密码重置失败", ApiErrorCodes.PASSWORD_CHANGE_FAILED);
            }

            LogOperation("重置用户密码", null, id);
            return Success("密码重置成功");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("用户不存在"))
        {
            return NotFound(ex.Message, ApiErrorCodes.USER_NOT_FOUND);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "重置密码", id);
        }
    }

    /// <summary>
    /// 用户修改密码 - 统一API响应格式
    /// </summary>
    [HttpPatch("password")]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var validation = ValidateModel();
            if (validation != null) return validation;

            var (operatorId, operatorName, _) = GetOperator();
            if (operatorId == Guid.Empty)
            {
                return Unauthorized("未登录或用户信息无效", ApiErrorCodes.AUTHENTICATION_FAILED);
            }

            var result = await _userService.ChangePasswordAsync(operatorId, dto.OldPassword, dto.NewPassword);
            if (!result)
            {
                return BusinessFail("密码修改失败，请检查当前密码", ApiErrorCodes.PASSWORD_CHANGE_FAILED);
            }

            LogOperation("修改密码", null, operatorId);
            return Success("密码修改成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "修改密码", dto);
        }
    }

    /// <summary>
    /// 用户修改个人信息 - 统一API响应格式
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse>> ChangeProfile([FromBody] ChangeProfileDto dto)
    {
        try
        {
            var validation = ValidateModel();
            if (validation != null) return validation;

            var (operatorId, operatorName, _) = GetOperator();
            if (operatorId == Guid.Empty)
            {
                return Unauthorized("未登录或用户信息无效", ApiErrorCodes.AUTHENTICATION_FAILED);
            }

            var result = await _userService.ChangeProfileAsync(operatorId, dto.RealName, dto.PhoneNumber);
            if (!result)
            {
                return BusinessFail("个人信息修改失败", ApiErrorCodes.DATA_UPDATE_FAILED);
            }

            LogOperation("修改个人信息", dto, operatorId);
            return Success("个人信息修改成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "修改个人信息", dto);
        }
    }

    /// <summary>
    /// 获取所有角色 - 统一API响应格式
    /// </summary>
    [HttpGet("getRoles")]
    public ActionResult<ApiResponse<IEnumerable<object>>> GetRoles()
    {
        try
        {
            var roles = _userService.GetRoles();
            return Success<IEnumerable<object>>(roles, "获取角色列表成功");
        }
        catch (Exception ex)
        {
            return HandleException<IEnumerable<object>>(ex, "获取角色列表", null);
        }
    }

    // 移除重复的GetById接口，统一使用RESTful GET /{id}接口

    /// <summary>
    /// 获取启用的用户列表 - 统一API响应格式
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LYBT.Shared.Models.Contracts.Users.UserDto>>>> GetActiveUsers()
    {
        try
        {
            var users = await _userService.GetActiveUsersAsync();
            return Success<IEnumerable<LYBT.Shared.Models.Contracts.Users.UserDto>>(users, "获取启用用户列表成功");
        }
        catch (Exception ex)
        {
            return HandleException<IEnumerable<LYBT.Shared.Models.Contracts.Users.UserDto>>(ex, "获取启用用户列表", null);
        }
    }

    // ======================== RESTful 标准接口 ========================

    /// <summary>
    /// 获取所有用户列表 (RESTful GET /Users) - 支持模糊查询 - 统一API响应格式
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<LYBT.Shared.Models.Contracts.Users.UserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? username = null,
        [FromQuery] string? realName = null,
        [FromQuery] string? email = null,
        [FromQuery] string? phoneNumber = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return ValidationFailPaged<LYBT.Shared.Models.Contracts.Users.UserDto>("页码和页大小参数无效（页码>0，页大小1-100）");
            }

            var (_, _, operatorRole) = GetOperator();
            var query = new LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto
            {
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
            return Success(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleExceptionPaged<LYBT.Shared.Models.Contracts.Users.UserDto>(ex, "获取用户列表", new { page, pageSize, keyword });
        }
    }

    /// <summary>
    /// 根据ID获取用户 (RESTful GET /Users/{id}) - 统一API响应格式
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LYBT.Shared.Models.Contracts.Users.UserDto>>> GetUser(Guid id)
    {
        try
        {
            var validation = ValidateGuid<LYBT.Shared.Models.Contracts.Users.UserDto>(id, "用户ID");
            if (validation != null) return validation;

            var (_, _, operatorRole) = GetOperator();
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound<LYBT.Shared.Models.Contracts.Users.UserDto>("用户不存在", ApiErrorCodes.USER_NOT_FOUND);
            }
            
            return Success(user, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<LYBT.Shared.Models.Contracts.Users.UserDto>(ex, "获取用户信息", id);
        }
    }

    /// <summary>
    /// 创建新用户 (RESTful POST /Users) - 统一API响应格式
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<LYBT.Shared.Models.Contracts.Users.UserDto>>> CreateUser([FromBody] LYBT.Shared.Models.Contracts.Users.UserCreateDto dto)
    {
        try
        {
            var validation = ValidateModel<LYBT.Shared.Models.Contracts.Users.UserDto>();
            if (validation != null) return validation;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _userService.AddAsync(dto, operatorId, operatorName);
            
            if (result == null)
            {
                return BusinessFail<LYBT.Shared.Models.Contracts.Users.UserDto>("用户创建失败", ApiErrorCodes.DATA_SAVE_FAILED);
            }

            LogOperation("创建用户", result, result.Id);
            return Success(result, "用户创建成功");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("已存在"))
        {
            return BusinessFail<LYBT.Shared.Models.Contracts.Users.UserDto>(ex.Message, ApiErrorCodes.USERNAME_EXISTS);
        }
        catch (Exception ex)
        {
            return HandleException<LYBT.Shared.Models.Contracts.Users.UserDto>(ex, "创建用户", dto);
        }
    }

    /// <summary>
    /// 更新用户信息 (RESTful PUT /Users/{id}) - 统一API响应格式
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<LYBT.Shared.Models.Contracts.Users.UserDto>>> UpdateUser(Guid id, [FromBody] LYBT.Shared.Models.Contracts.Users.UserUpdateDto dto)
    {
        try
        {
            var idValidation = ValidateGuid<LYBT.Shared.Models.Contracts.Users.UserDto>(id, "用户ID");
            if (idValidation != null) return idValidation;

            var modelValidation = ValidateModel<LYBT.Shared.Models.Contracts.Users.UserDto>();
            if (modelValidation != null) return modelValidation;

            // 检查ID一致性
            if (dto.Id != id)
            {
                return ValidationFail<LYBT.Shared.Models.Contracts.Users.UserDto>("URL中的ID与请求体中的ID不匹配");
            }

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _userService.UpdateAsync(dto, operatorId, operatorName);
            
            if (!result)
            {
                return BusinessFail<LYBT.Shared.Models.Contracts.Users.UserDto>("用户信息更新失败", ApiErrorCodes.DATA_UPDATE_FAILED);
            }

            // 获取更新后的资源
            var updated = await _userService.GetByIdAsync(dto.Id);
            LogOperation("更新用户信息", updated, dto.Id);
            return Success(updated!, "用户信息更新成功");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("用户不存在"))
        {
            return NotFound<LYBT.Shared.Models.Contracts.Users.UserDto>(ex.Message, ApiErrorCodes.USER_NOT_FOUND);
        }
        catch (Exception ex)
        {
            return HandleException<LYBT.Shared.Models.Contracts.Users.UserDto>(ex, "更新用户信息", new { id, dto });
        }
    }

    // 注意：本系统采用软删除策略，不提供DELETE接口
    // 请使用 PATCH /Users/{id}/toggle-status 来切换用户状态
}