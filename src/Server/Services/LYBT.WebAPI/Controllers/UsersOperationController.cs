using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 用户批量操作 API 控制器 - 处理批量启用、禁用、密码重置等业务操作
    /// 对应 IUserBusinessService 的业务功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/users/operation")]
    [Authorize(Roles = "Admin")]  // 仅管理员可访问用户管理操作
    public class UsersOperationController : BaseApiController
    {
        private readonly IUserBusinessService _businessService;

        /// <summary>
        /// 构造方法，注入用户业务服务
        /// </summary>
        public UsersOperationController(
            IUserBusinessService businessService,
            IMemoryCache memoryCache,
            ILogger<UsersOperationController> logger)
            : base(logger, memoryCache)
        {
            _businessService = businessService;
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        [HttpPost("batch-enable")]
        public async Task<ActionResult<ApiResponse<int>>> BatchEnable([FromBody] List<Guid> userIds)
        {
            try
            {
                if (userIds == null || userIds.Count == 0)
                {
                    return ValidationFail<int>("用户ID列表不能为空", "INVALID_USER_IDS");
                }

                var result = await _businessService.BatchEnableAsync(userIds);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<int>(result.ErrorMessage ?? "批量启用失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("批量启用用户", new { Count = result.Data, UserIds = userIds }, null);
                return Success(result.Data, $"成功启用 {result.Data} 个用户");
            }
            catch (Exception ex)
            {
                return HandleException<int>(ex, "批量启用用户", userIds);
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        [HttpPost("batch-disable")]
        public async Task<ActionResult<ApiResponse<int>>> BatchDisable([FromBody] List<Guid> userIds)
        {
            try
            {
                if (userIds == null || userIds.Count == 0)
                {
                    return ValidationFail<int>("用户ID列表不能为空", "INVALID_USER_IDS");
                }

                var result = await _businessService.BatchDisableAsync(userIds);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<int>(result.ErrorMessage ?? "批量禁用失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("批量禁用用户", new { Count = result.Data, UserIds = userIds }, null);
                return Success(result.Data, $"成功禁用 {result.Data} 个用户");
            }
            catch (Exception ex)
            {
                return HandleException<int>(ex, "批量禁用用户", userIds);
            }
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        [HttpPost("{userId:guid}/reset-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(Guid userId, [FromBody] UserResetPasswordDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.NewPassword))
                {
                    return ValidationFail<bool>("新密码不能为空", "INVALID_PASSWORD");
                }

                var result = await _businessService.ResetPasswordAsync(userId, dto.NewPassword);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "重置密码失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("重置用户密码", new { UserId = userId }, userId);
                return Success(result.Data, "密码重置成功");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "重置用户密码", new { userId, dto });
            }
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        [HttpPost("{userId:guid}/change-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(Guid userId, [FromBody] UserChangePasswordDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ValidationFail<bool>("请求数据不能为空", "INVALID_REQUEST");
                }

                if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                {
                    return ValidationFail<bool>("旧密码和新密码都不能为空", "INVALID_PASSWORD");
                }

                var result = await _businessService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "修改密码失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("修改用户密码", new { UserId = userId }, userId);
                return Success(result.Data, "密码修改成功");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "修改用户密码", new { userId, dto });
            }
        }

        /// <summary>
        /// 修改用户个人信息
        /// </summary>
        [HttpPost("{userId:guid}/change-profile")]
        public async Task<ActionResult<ApiResponse<bool>>> ChangeProfile(Guid userId, [FromBody] UserChangeProfileDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ValidationFail<bool>("请求数据不能为空", "INVALID_REQUEST");
                }

                if (string.IsNullOrWhiteSpace(dto.RealName))
                {
                    return ValidationFail<bool>("真实姓名不能为空", "INVALID_REALNAME");
                }

                var result = await _businessService.ChangeProfileAsync(userId, dto.RealName, dto.PhoneNumber ?? string.Empty);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "修改个人信息失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("修改用户个人信息", new { UserId = userId, dto }, userId);
                return Success(result.Data, "个人信息修改成功");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "修改用户个人信息", new { userId, dto });
            }
        }
    }

    /// <summary>
    /// 重置密码DTO
    /// </summary>
    public class UserResetPasswordDto
    {
        /// <summary>
        /// 新密码
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 修改密码DTO
    /// </summary>
    public class UserChangePasswordDto
    {
        /// <summary>
        /// 旧密码
        /// </summary>
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// 新密码
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 修改个人信息DTO
    /// </summary>
    public class UserChangeProfileDto
    {
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>
        /// 电话号码
        /// </summary>
        public string? PhoneNumber { get; set; }
    }
}