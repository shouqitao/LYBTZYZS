using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 用户管理控制器 - 简化版（仅CRUD）
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService, ILogger<UsersController> logger, IMemoryCache? cache = null)
            : base(logger, cache)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// 获取用户列表（分页）（Issue #1162: 支持角色和状态筛选）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="role">角色筛选</param>
        /// <param name="status">状态筛选</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null)
        {
            try
            {
                var result = await _userService.GetPagedAsync(page, pageSize, keyword, role, status);
                return HandlePagedServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<UserDto>(ex, "获取用户列表");
            }
        }

        /// <summary>
        /// 获取当前登录用户信息
        /// </summary>
        [HttpGet("current")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized<UserDto>("无法获取当前用户信息");
                }

                // 特殊处理超级管理员
                if (userId == Guid.Empty)
                {
                    var username = User.Identity?.Name ?? "sysadmin";
                    var isSuperAdmin = User.FindFirst("IsSuperAdmin")?.Value == "true";
                    
                    if (isSuperAdmin)
                    {
                        // 返回超级管理员的虚拟用户信息
                        var superAdminDto = new UserDto
                        {
                            Id = Guid.Empty,
                            UserName = username,
                            RealName = "系统超级管理员",
                            Role = UserRole.Admin,
                            Email = "admin@lybt.com",
                            Status = CommonStatus.Enabled,
                            CreatedAt = DateTime.MinValue,
                            UpdatedAt = DateTime.Now
                        };
                        return Success(superAdminDto);
                    }
                }

                var result = await _userService.GetByIdAsync(userId);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<UserDto>(ex, "获取当前用户信息");
            }
        }

        /// <summary>
        /// 获取单个用户
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<UserDto>(id, "用户ID");
                if (validationResult != null) return validationResult;

                var result = await _userService.GetByIdAsync(id);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<UserDto>(ex, "获取用户", new { UserId = id });
            }
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] UserCreateDto dto)
        {
            try
            {
                var validationResult = ValidateModel<UserDto>();
                if (validationResult != null) return validationResult;

                var result = await _userService.CreateAsync(dto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建用户", dto, result.Data.Id);
                    return CreatedAtAction(nameof(GetUser), new { id = result.Data.Id },
                        ApiResponse<UserDto>.CreateSuccess(result.Data));
                }
                
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<UserDto>(ex, "创建用户", dto);
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UserUpdateDto dto)
        {
            try
            {
                var guidValidationResult = ValidateGuid<UserDto>(id, "用户ID");
                if (guidValidationResult != null) return guidValidationResult;

                var modelValidationResult = ValidateModel<UserDto>();
                if (modelValidationResult != null) return modelValidationResult;

                var result = await _userService.UpdateAsync(id, dto);
                
                if (result.IsSuccess)
                {
                    LogOperation("更新用户", dto, id);
                }
                
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<UserDto>(ex, "更新用户", new { UserId = id, UpdateData = dto });
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse>> DeleteUser(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "用户ID");
                if (validationResult != null) return validationResult;

                var result = await _userService.DeleteAsync(id);

                if (result.IsSuccess)
                {
                    LogOperation("删除用户", null, id);
                }

                return HandleServiceResult(result, "删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除用户", new { UserId = id });
            }
        }

        /// <summary>
        /// 管理员重置用户密码 (Issue #1162)
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="request">重置密码请求（新密码可选，不提供则自动生成）</param>
        [HttpPost("{id:guid}/reset-password")]
        [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<ResetPasswordResponseDto>>> ResetPassword(
            Guid id,
            [FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                var validationResult = ValidateGuid<ResetPasswordResponseDto>(id, "用户ID");
                if (validationResult != null) return validationResult;

                var result = await _userService.ResetPasswordAsync(id, request);

                if (result.IsSuccess)
                {
                    LogOperation("重置用户密码", new { AutoGenerated = string.IsNullOrEmpty(request.NewPassword) }, id);
                }

                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<ResetPasswordResponseDto>(ex, "重置用户密码", new { UserId = id });
            }
        }

        /// <summary>
        /// 切换用户状态（启用/禁用）(Issue #1162)
        /// </summary>
        /// <param name="id">用户ID</param>
        [HttpPost("{id:guid}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> ToggleStatus(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<UserDto>(id, "用户ID");
                if (validationResult != null) return validationResult;

                var result = await _userService.ToggleStatusAsync(id);

                if (result.IsSuccess)
                {
                    LogOperation("切换用户状态", new { NewStatus = result.Data?.Status }, id);
                }

                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<UserDto>(ex, "切换用户状态", new { UserId = id });
            }
        }
    }
}