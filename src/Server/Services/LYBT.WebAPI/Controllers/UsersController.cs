using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 用户管理控制器 - 统一API响应格式和错误处理
/// 实现软删除策略：用户只能禁用/启用，不提供删除接口
/// </summary>
[ApiController]
[ApiVersion("1")]
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
            if (validation != null)
            {
                return validation;
            }

            var (operatorId, operatorName, operatorRole) = GetOperator();

            // 先获取用户当前状态
            var userResult = await _userService.GetByIdAsync(id);
            if (!userResult.IsSuccess || userResult.Data == null)
            {
                return NotFound("用户不存在", ApiErrorCodes.USERNOTFOUND);
            }

            // 根据当前状态切换
            ServiceResult<bool> result;
            string message;
            if (userResult.Data.Status == CommonStatus.Enabled)
            {
                result = await _userService.DisableAsync(id);
                message = "用户已禁用";
            }
            else
            {
                result = await _userService.EnableAsync(id);
                message = "用户已启用";
            }

            if (!result.IsSuccess || !result.Data)
            {
                return BusinessFail("状态切换失败", ApiErrorCodes.DATAUPDATEFAILED);
            }

            LogOperation(message, null, id);
            return Success(message);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "切换用户状态", id);
        }
    }

    // UltraThink v2.0: 删除批量操作功能 - 20人以下小诊所不需要复杂的批量启用/禁用功能
    // 已删除 BatchDisable 和 BatchEnable 方法，使用 ToggleStatus 单个操作替代

    /// <summary>
    /// 管理员重置密码，恢复为默认值 - 统一API响应格式
    /// </summary>
    [HttpPost("reset-password/{id}")]
    public async Task<ActionResult<ApiResponse>> ResetPassword(Guid id)
    {
        try
        {
            var validation = ValidateGuid(id, "用户ID");
            if (validation != null)
            {
                return validation;
            }

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _userService.ResetPasswordAsync(id, "ChangeMe123");

            if (!result.IsSuccess || !result.Data)
            {
                return BusinessFail("密码重置失败", ApiErrorCodes.PASSWORDCHANGEFAILED);
            }

            LogOperation("重置用户密码", null, id);
            return Success("密码重置成功");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("用户不存在"))
        {
            return NotFound(ex.Message, ApiErrorCodes.USERNOTFOUND);
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
            if (validation != null)
            {
                return validation;
            }

            var (operatorId, operatorName, _) = GetOperator();
            if (operatorId == Guid.Empty)
            {
                return Unauthorized("未登录或用户信息无效", ApiErrorCodes.AUTHENTICATIONFAILED);
            }

            var result = await _userService.ChangePasswordAsync(operatorId, dto.OldPassword, dto.NewPassword);
            if (!result.IsSuccess || !result.Data)
            {
                return BusinessFail("密码修改失败，请检查当前密码", ApiErrorCodes.PASSWORDCHANGEFAILED);
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
    /// 获取当前用户个人信息 - 统一API响应格式
    /// UltraThink修复：支持sysadmin特殊用户处理
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
    {
        try
        {
            var (operatorId, operatorName, _) = GetOperator();
            if (operatorId == Guid.Empty)
            {
                return Unauthorized<UserDto>("未登录或用户信息无效", ApiErrorCodes.AUTHENTICATIONFAILED);
            }

            // UltraThink修复：检查是否为sysadmin用户（使用固定ID）
            var sysadminId = new Guid("00000000-0000-0000-0000-000000000001");
            if (operatorId == sysadminId)
            {
                // 对于sysadmin用户，创建虚拟UserDto
                var sysadminDto = new UserDto
                {
                    Id = sysadminId,
                    Username = "sysadmin",
                    RealName = "系统管理员",
                    Role = "Admin",
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
                    // IsActive是计算属性，不需要设置
                };

                LogOperation("获取个人信息", null, operatorId);
                return Success(sysadminDto, "获取个人信息成功");
            }

            // 普通用户正常流程
            var result = await _userService.GetByIdAsync(operatorId);
            if (!result.IsSuccess || result.Data == null)
            {
                return NotFound<UserDto>("用户不存在", ApiErrorCodes.USERNOTFOUND);
            }

            LogOperation("获取个人信息", null, operatorId);
            return Success(result.Data, "获取个人信息成功");
        }
        catch (Exception ex)
        {
            return HandleException<UserDto>(ex, "获取个人信息", null);
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
            if (validation != null)
            {
                return validation;
            }

            var (operatorId, operatorName, _) = GetOperator();
            if (operatorId == Guid.Empty)
            {
                return Unauthorized("未登录或用户信息无效", ApiErrorCodes.AUTHENTICATIONFAILED);
            }

            dto.UserId = operatorId; // 设置当前操作用户ID
            var result = await _userService.ChangeProfileAsync(dto);
            if (!result.IsSuccess || !result.Data)
            {
                return BusinessFail("个人信息修改失败", ApiErrorCodes.DATAUPDATEFAILED);
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
    [HttpGet("roles")]
    public ActionResult<ApiResponse<IEnumerable<object>>> GetRoles()
    {
        try
        {
            // 临时返回固定角色列表，实际应该从配置中获取
            var roles = new[] {
                new { Value = "Admin", Label = "管理员" },
                new { Value = "Doctor", Label = "医生" },
                new { Value = "Receptionist", Label = "接待员" }
            };
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
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetActiveUsers()
    {
        try
        {
            var usersResult = await _userService.GetActiveUsersAsync();
            if (!usersResult.IsSuccess || usersResult.Data == null)
            {
                return HandleException<IEnumerable<UserDto>>(new Exception(usersResult.ErrorMessage ?? "获取用户列表失败"), "获取启用用户列表", null);
            }
            return Success<IEnumerable<UserDto>>(usersResult.Data, "获取启用用户列表成功");
        }
        catch (Exception ex)
        {
            return HandleException<IEnumerable<UserDto>>(ex, "获取启用用户列表", null);
        }
    }

    // ======================== RESTful 标准接口 ========================

    /// <summary>
    /// 获取所有用户列表 (RESTful GET /Users) - 支持模糊查询 - 统一API响应格式
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
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
                return ValidationFailPaged<UserDto>("页码和页大小参数无效（页码>0，页大小1-100）");
            }

            var (_, _, operatorRole) = GetOperator();
            var query = new LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto
            {
                PageIndex = page,
                PageSize = pageSize,
                Keyword = keyword,
                Username = username,
                RealName = realName,
                Email = email,
                PhoneNumber = phoneNumber,
                // Role = role ?? string.Empty, // Role字段已移除
                Status = isActive.HasValue ? (isActive.Value ? CommonStatus.Enabled : CommonStatus.Disabled) : (CommonStatus?)null
            };

            var result = await _userService.GetPagedAsync(query);
            return HandlePagedServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleExceptionPaged<UserDto>(ex, "获取用户列表", new { page, pageSize, keyword });
        }
    }

    /// <summary>
    /// 根据ID获取用户 (RESTful GET /Users/{id}) - 统一API响应格式
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        try
        {
            var validation = ValidateGuid<UserDto>(id, "用户ID");
            if (validation != null)
            {
                return validation;
            }

            var (_, _, operatorRole) = GetOperator();
            var result = await _userService.GetByIdAsync(id);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<UserDto>(ex, "获取用户信息", id);
        }
    }

    /// <summary>
    /// 创建新用户 (现代化版本，直接使用UserMutationDto) - 统一API响应格式
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] UserMutationDto dto)
    {
        try
        {
            var validation = ValidateModel<UserDto>();
            if (validation != null)
            {
                return validation;
            }

            dto.IsCreateOperation = true; // 标记为创建操作
            var result = await _userService.CreateAsync(dto);

            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("创建用户", result.Data, result.Data.Id);
            }
            return HandleServiceResult(result, "用户创建成功");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("已存在"))
        {
            return BusinessFail<UserDto>(ex.Message, ApiErrorCodes.USERNAMEEXISTS);
        }
        catch (Exception ex)
        {
            return HandleException<UserDto>(ex, "创建用户", dto);
        }
    }

    /// <summary>
    /// 更新用户信息 (现代化版本，直接使用UserMutationDto) - 统一API响应格式
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UserMutationDto dto)
    {
        try
        {
            var idValidation = ValidateGuid<UserDto>(id, "用户ID");
            if (idValidation != null)
            {
                return idValidation;
            }

            var modelValidation = ValidateModel<UserDto>();
            if (modelValidation != null)
            {
                return modelValidation;
            }

            // 检查ID一致性
            if (dto.Id != id)
            {
                return ValidationFail<UserDto>("URL中的ID与请求体中的ID不匹配");
            }

            dto.IsCreateOperation = false; // 标记为更新操作
            var result = await _userService.UpdateAsync(dto);

            return HandleServiceResult(result, "用户信息更新成功");
        }
        catch (Exception ex)
        {
            return HandleException<UserDto>(ex, "更新用户信息", new { id, dto });
        }
    }

    // 注意：本系统采用软删除策略，不提供DELETE接口
    // 请使用 PATCH /Users/{id}/toggle-status 来切换用户状态
}
