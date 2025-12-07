using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 用户管理控制器 - 简化版（仅CRUD）
    /// </summary>
    /// optimize-api-permissions: 用户管理仅限Admin角色访问
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public UsersController(IUserService userService, IConfiguration configuration, ILogger<UsersController> logger)
            : base(logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// 获取用户列表（分页）
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetUsers(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null)
        {
            try
            {
                var result = await _userService.GetPagedAsync(page, pageSize, keyword, role, status);
                return SuccessPaged(result.Data!, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取用户列表");
            }
        }

        /// <summary>
        /// 获取当前登录用户信息
        /// </summary>
        [HttpGet("current")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("无法获取当前用户信息");
                }

                if (userId == Guid.Empty)
                {
                    var username = User.Identity?.Name ?? "sysadmin";
                    var isSuperAdmin = User.FindFirst("IsSuperAdmin")?.Value == "true";

                    if (isSuperAdmin)
                    {
                        var superAdminDto = new UserDto
                        {
                            Id = Guid.Empty,
                            UserName = username,
                            RealName = "系统超级管理员",
                            Role = UserRole.Admin,
                            Email = _configuration["Lybt:SystemAdmin:Email"]
                                ?? throw new InvalidOperationException("未配置系统管理员Email: Lybt:SystemAdmin:Email"),
                            Status = CommonStatus.Enabled,
                            CreatedAt = DateTime.MinValue,
                            UpdatedAt = DateTime.Now
                        };
                        return Success(superAdminDto);
                    }
                }

                var result = await _userService.GetByIdAsync(userId);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound(result.ErrorMessage ?? "用户不存在");
                }
                return Success(result.Data);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取当前用户信息");
            }
        }

        /// <summary>
        /// 获取单个用户
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "用户ID") is { } error) return error;

                var result = await _userService.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound(result.ErrorMessage ?? "用户不存在");
                }
                return Success(result.Data);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取用户", new { UserId = id });
            }
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateUser([FromBody] UserInputDto dto)
        {
            try
            {
                var result = await _userService.CreateAsync(dto);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建用户", dto, result.Data.Id);
                    return CreatedAtAction(nameof(GetUser),
                        new { id = result.Data.Id, version = "1" },
                        ApiResponse<UserDto>.CreateSuccess(result.Data, "创建成功"));
                }

                return BusinessFail(result.ErrorMessage ?? "创建用户失败");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "创建用户", dto);
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserInputDto dto)
        {
            try
            {
                if (ValidateGuid(id, "用户ID") is { } error) return error;

                var result = await _userService.UpdateAsync(id, dto);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("更新用户", dto, id);
                    return Success(result.Data, "用户更新成功");
                }

                return BusinessFail(result.ErrorMessage ?? "更新用户失败");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新用户", new { UserId = id, UpdateData = dto });
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "用户ID") is { } error) return error;

                var result = await _userService.DeleteAsync(id);

                if (result.IsSuccess)
                {
                    LogOperation("删除用户", null, id);
                    return Success("删除成功");
                }

                return NotFound(result.ErrorMessage ?? "用户不存在");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除用户", new { UserId = id });
            }
        }

        /// <summary>
        /// 管理员重置用户密码
        /// </summary>
        [HttpPost("{id:guid}/reset-password")]
        [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                if (ValidateGuid(id, "用户ID") is { } error) return error;

                var result = await _userService.ResetPasswordAsync(id, request);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("重置用户密码", new { AutoGenerated = true }, id);
                    return Success(result.Data, "密码重置成功");
                }

                return BusinessFail(result.ErrorMessage ?? "密码重置失败");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "重置用户密码", new { UserId = id });
            }
        }

        /// <summary>
        /// 修改个人资料
        /// </summary>
        [HttpPut("{id:guid}/profile")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ChangeProfile(Guid id, [FromBody] ChangeProfileDto dto)
        {
            try
            {
                if (ValidateGuid(id, "用户ID") is { } error) return error;

                var result = await _userService.ChangeProfileAsync(id, dto);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("修改个人资料", new { RealName = dto.RealName, PhoneNumber = dto.PhoneNumber }, id);
                    return Success(result.Data, "个人资料修改成功");
                }

                return BusinessFail(result.ErrorMessage ?? "个人资料修改失败");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "修改个人资料", new { UserId = id, ProfileData = dto });
            }
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        [HttpPut("{id:guid}/change-password")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request)
        {
            try
            {
                if (ValidateGuid(id, "用户ID") is { } error) return error;

                var result = await _userService.ChangePasswordAsync(id, request.OldPassword, request.NewPassword);

                if (result.IsSuccess)
                {
                    LogOperation("修改密码", new { UserId = id }, id);
                    return Success("密码修改成功");
                }

                return Error(result.ErrorMessage ?? "密码修改失败");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "修改密码", new { UserId = id });
            }
        }

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复端点 ==========

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        [HttpPost("{id:guid}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "用户ID") is { } error) return error;

                var result = await _userService.ToggleStatusAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "状态切换失败");
                }

                LogOperation("切换用户状态", new { NewStatus = result.Data.Status }, id);
                return Success(result.Data, $"用户已{(result.Data.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "切换用户状态", new { UserId = id });
            }
        }

        /// <summary>
        /// 恢复已删除的用户
        /// </summary>
        [HttpPost("{id:guid}/restore")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Restore(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "用户ID") is { } error) return error;

                var result = await _userService.RestoreAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "恢复失败");
                }

                LogOperation("恢复用户", null, id);
                return Success(result.Data, "用户已恢复");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "恢复用户", new { UserId = id });
            }
        }
    }
}
