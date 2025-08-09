using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 用户管理控制器 - 前后端契约统一化示例
    /// 演示标准化API响应格式、错误处理和DTO使用规范
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/standardized/[controller]")]
    [Authorize]
    public class StandardizedUsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public StandardizedUsersController(
            IUserService userService, 
            IMemoryCache cache, 
            ILogger<StandardizedUsersController> logger)
            : base(logger, cache)
        {
            _userService = userService;
        }

        /// <summary>
        /// 获取用户分页列表 - 标准化分页响应
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedApiResponse<UserDto>>> GetPagedUsers([FromQuery] UserPagedQueryDto query)
        {
            try
            {
                // 验证查询参数
                var validation = ValidateModelPaged<UserDto>();
                if (validation != null) return validation;

                // 调用服务层
                var result = await _userService.GetPagedAsync(query);

                // 返回标准化分页响应
                return Success(result, "用户列表查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<UserDto>(ex, "查询用户列表", query);
            }
        }

        /// <summary>
        /// 根据ID获取用户详情 - 标准化单个资源响应
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUserById(Guid id)
        {
            try
            {
                // 验证参数
                var validation = ValidateGuid<UserDetailDto>(id, "用户ID");
                if (validation != null) return validation;

                // 查询用户
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound<UserDetailDto>("用户不存在", ApiErrorCodes.USER_NOT_FOUND);
                }

                // 映射到详情DTO（实际应用中可能需要更详细的映射）
                var detailDto = new UserDetailDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    RealName = user.RealName,
                    PhoneNumber = user.PhoneNumber,
                    Status = user.Status,
                    CreateTime = user.CreateTime
                    // 更多详情字段...
                };

                return Success(detailDto, "用户详情查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<UserDetailDto>(ex, "查询用户详情", id);
            }
        }

        /// <summary>
        /// 创建新用户 - 标准化创建响应
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] UserCreateDto dto)
        {
            try
            {
                // 验证模型
                var validation = ValidateModel<UserDto>();
                if (validation != null) return validation;

                // 获取操作者信息
                var (operatorId, operatorName, _) = GetOperator();

                // 创建用户
                var user = await _userService.AddAsync(dto, operatorId, operatorName);
                if (user == null)
                {
                    return BusinessFail<UserDto>("用户创建失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                // 记录操作日志
                LogOperation("创建用户", user, user.Id);

                return Success(user, "用户创建成功");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("用户名已存在"))
            {
                return BusinessFail<UserDto>(ex.Message, ApiErrorCodes.USERNAME_EXISTS);
            }
            catch (Exception ex)
            {
                return HandleException<UserDto>(ex, "创建用户", dto);
            }
        }

        /// <summary>
        /// 更新用户信息 - 标准化更新响应
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UserUpdateDto dto)
        {
            try
            {
                // 验证参数
                var idValidation = ValidateGuid<UserDto>(id, "用户ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<UserDto>();
                if (modelValidation != null) return modelValidation;

                // 检查ID一致性
                if (dto.Id != id)
                {
                    return ValidationFail<UserDto>("URL中的ID与请求体中的ID不匹配");
                }

                // 获取操作者信息
                var (operatorId, operatorName, _) = GetOperator();

                // 更新用户
                var success = await _userService.UpdateAsync(dto, operatorId, operatorName);
                if (!success)
                {
                    return BusinessFail<UserDto>("用户更新失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                // 返回更新后的用户信息
                var updatedUser = await _userService.GetByIdAsync(id);
                LogOperation("更新用户", updatedUser, id);

                return Success(updatedUser!, "用户更新成功");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("用户不存在"))
            {
                return NotFound<UserDto>(ex.Message, ApiErrorCodes.USER_NOT_FOUND);
            }
            catch (Exception ex)
            {
                return HandleException<UserDto>(ex, "更新用户", new { id, dto });
            }
        }

        /// <summary>
        /// 切换用户状态 - 标准化状态操作响应
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<ApiResponse>> ToggleUserStatus(Guid id)
        {
            try
            {
                // 验证参数
                var validation = ValidateGuid(id, "用户ID");
                if (validation != null) return validation;

                // 获取操作者信息
                var (operatorId, operatorName, _) = GetOperator();

                // 获取当前用户状态
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound("用户不存在", ApiErrorCodes.USER_NOT_FOUND);
                }

                // 根据当前状态切换
                bool result;
                string message;
                if (user.Status == Shared.Models.Enums.CommonStatus.Enabled)
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
        /// 批量启用用户 - 标准化批量操作响应
        /// </summary>
        [HttpPatch("batch/enable")]
        public async Task<ActionResult<ApiResponse<BatchOperationResult>>> BatchEnableUsers([FromBody] List<Guid> ids)
        {
            try
            {
                // 验证参数
                if (ids == null || ids.Count == 0)
                {
                    return ValidationFail<BatchOperationResult>("用户ID列表不能为空");
                }

                if (ids.Count > 100) // 限制批量操作数量
                {
                    return ValidationFail<BatchOperationResult>("批量操作数量不能超过100个");
                }

                // 获取操作者信息
                var (operatorId, operatorName, _) = GetOperator();

                // 执行批量启用
                var successCount = await _userService.BatchEnableAsync(ids, operatorId, operatorName);

                var result = new BatchOperationResult
                {
                    TotalCount = ids.Count,
                    SuccessCount = successCount,
                    FailedCount = ids.Count - successCount
                };

                LogOperation($"批量启用用户，成功{successCount}个，失败{result.FailedCount}个", result);

                return Success(result, $"批量启用操作完成，成功{successCount}个");
            }
            catch (Exception ex)
            {
                return HandleException<BatchOperationResult>(ex, "批量启用用户", ids);
            }
        }

        /// <summary>
        /// 重置用户密码 - 标准化密码操作响应
        /// </summary>
        [HttpPost("{id}/reset-password")]
        public async Task<ActionResult<ApiResponse>> ResetUserPassword(Guid id)
        {
            try
            {
                // 验证参数
                var validation = ValidateGuid(id, "用户ID");
                if (validation != null) return validation;

                // 获取操作者信息
                var (operatorId, operatorName, _) = GetOperator();

                // 重置密码
                var success = await _userService.ResetPasswordAsync(id, operatorId, operatorName);
                if (!success)
                {
                    return BusinessFail("密码重置失败", ApiErrorCodes.DATA_UPDATE_FAILED);
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
                return HandleException(ex, "重置用户密码", id);
            }
        }
    }

    /// <summary>
    /// 批量操作结果DTO
    /// </summary>
    public class BatchOperationResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
    }

    /// <summary>
    /// 用户详情DTO示例（实际项目中应在Shared项目中定义）
    /// </summary>
    public class UserDetailDto : UserDto
    {
        public string? PinYinCode { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public DateTime CreateTime { get; set; }
        // 更多详情字段...
    }
}