using Asp.Versioning;
using LYBT.Infrastructure.Constants;
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
    /// S2: 类级 [Authorize] 允许所有认证用户访问自服务端点
    /// 管理端点通过方法级 [Authorize(Policy = PolicyConstants.AdminOnly)] 限制
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/users")]
    [Authorize]
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
        /// OpenSpec: refactor-dto-simplification - 使用扁平化DTO
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PolicyConstants.AdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserListDto>>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetList(
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var result = await _userService.GetPagedAsync(page, pageSize, keyword, role, status, cancellationToken);
            return SuccessPaged(result.Data!, "查询成功");
        }

        /// <summary>
        /// 获取当前登录用户信息
        /// </summary>
        [HttpGet("current")]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
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
                    var superAdminDto = new UserDetailDto
                    {
                        Id = Guid.Empty,
                        UserName = username,
                        RealName = "系统超级管理员",
                        Role = UserRole.Admin,
                        Email = _configuration["Lybt:SystemAdmin:Email"]
                            ?? throw new InvalidOperationException("未配置系统管理员Email: Lybt:SystemAdmin:Email"),
                        Status = CommonStatus.Enabled,
                        CreatedAt = DateTime.MinValue,
                        UpdatedAt = DateTime.UtcNow
                    };
                    return Success(superAdminDto);
                }
            }

            var result = await _userService.GetByIdAsync(userId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return NotFound(result.ErrorMessage ?? "用户不存在");
            }
            return Success(result.Data);
        }

        /// <summary>
        /// 获取单个用户
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PolicyConstants.AdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var result = await _userService.GetByIdAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return NotFound(result.ErrorMessage ?? "用户不存在");
            }
            return Success(result.Data);
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PolicyConstants.AdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] UserInputDto dto, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var result = await _userService.CreateAsync(dto);

            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("创建用户", dto, result.Data.Id);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Data.Id, version = "1" },
                    ApiResponse<UserDetailDto>.CreateSuccess(result.Data, "创建成功"));
            }

            return BusinessFail(result.ErrorMessage ?? "创建用户失败");
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PolicyConstants.AdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UserInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var result = await _userService.UpdateAsync(id, dto);

            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("更新用户", dto, id);
                return Success(result.Data, "用户更新成功");
            }

            return BusinessFail(result.ErrorMessage ?? "更新用户失败");
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PolicyConstants.AdminOnly)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var result = await _userService.DeleteAsync(id);

            if (result.IsSuccess)
            {
                LogOperation("删除用户", null, id);
                return Success("删除成功");
            }

            return NotFound(result.ErrorMessage ?? "用户不存在");
        }

        /// <summary>
        /// 管理员重置用户密码
        /// </summary>
        [HttpPost("{id:guid}/reset-password")]
        [Authorize(Policy = PolicyConstants.SuperAdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequestDto request)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var result = await _userService.ResetPasswordAsync(id, request);

            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("重置用户密码", new { AutoGenerated = true }, id);
                return Success(result.Data, "密码重置成功");
            }

            return BusinessFail(result.ErrorMessage ?? "密码重置失败");
        }

        /// <summary>
        /// 修改个人资料
        /// </summary>
        [HttpPut("{id:guid}/profile")]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ChangeProfile(Guid id, [FromBody] ChangeProfileDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var result = await _userService.ChangeProfileAsync(id, dto);

            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("修改个人资料", new { RealName = dto.RealName, PhoneNumber = dto.PhoneNumber }, id);
                return Success(result.Data, "个人资料修改成功");
            }

            return BusinessFail(result.ErrorMessage ?? "个人资料修改失败");
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
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var result = await _userService.ChangePasswordAsync(id, request.OldPassword, request.NewPassword);

            if (result.IsSuccess)
            {
                LogOperation("修改密码", new { UserId = id }, id);
                return Success("密码修改成功");
            }

            return Error(result.ErrorMessage ?? "密码修改失败");
        }

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复端点 ==========

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        [HttpPost("{id:guid}/toggle-status")]
        [Authorize(Policy = PolicyConstants.AdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var result = await _userService.ToggleStatusAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "状态切换失败");
            }

            LogOperation("切换用户状态", new { NewStatus = result.Data.Status }, id);
            return Success(result.Data, $"用户已{(result.Data.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 恢复已删除的用户
        /// </summary>
        [HttpPost("{id:guid}/restore")]
        [Authorize(Policy = PolicyConstants.SuperAdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Restore(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "用户ID") is { } error) return error;

            var result = await _userService.RestoreAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "恢复失败");
            }

            LogOperation("恢复用户", null, id);
            return Success(result.Data, "用户已恢复");
        }


        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除用户
        /// </summary>
        [HttpPost("batch-delete")]
        [Authorize(Policy = PolicyConstants.AdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个用户");
            }

            // 获取当前用户ID，防止删除自己
            Guid? currentUserId = null;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var result = await _userService.BatchDeleteAsync(dto.Ids, currentUserId);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "批量删除失败");
            }

            LogOperation("批量删除用户", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }


        /// <summary>
        /// 批量启用用户
        /// </summary>
        [HttpPost("batch-enable")]
        [Authorize(Policy = PolicyConstants.AdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个用户");
            }

            Guid? currentUserId = null;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var result = await _userService.BatchUpdateStatusAsync(dto.Ids, CommonStatus.Enabled, currentUserId);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "批量启用失败");
            }

            LogOperation("批量启用用户", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        [HttpPost("batch-disable")]
        [Authorize(Policy = PolicyConstants.AdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个用户");
            }

            Guid? currentUserId = null;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var result = await _userService.BatchUpdateStatusAsync(dto.Ids, CommonStatus.Disabled, currentUserId);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "批量禁用失败");
            }

            LogOperation("批量禁用用户", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }
    }
}
