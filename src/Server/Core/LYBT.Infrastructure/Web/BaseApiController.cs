using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LYBT.Infrastructure.Constants;
using LYBT.Shared.Logging.Masking;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Infrastructure.Web
{
    /// <summary>
    /// API控制器基类 - 统一返回IActionResult
    /// 设计原则：
    /// - 所有响应方法返回IActionResult，消除泛型/非泛型重复
    /// - 统一使用ApiResponse包装响应数据
    /// - 简洁的方法命名，无重复
    /// </summary>
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly ILogger _logger;

        protected BaseApiController(ILogger logger)
        {
            _logger = logger;
        }

        #region 核心通用功能

        /// <summary>
        /// 获取当前操作者信息 - 兼容多种Claims标准
        /// </summary>
        protected (Guid OperatorId, string OperatorName, UserRole OperatorRole) GetOperator()
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                        ?? User?.FindFirst("sub")?.Value;

            var userName = User?.Identity?.Name
                          ?? User?.FindFirst(ClaimTypes.Name)?.Value
                          ?? User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                          ?? User?.FindFirst("unique_name")?.Value
                          ?? User?.FindFirst("name")?.Value;

            var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value
                         ?? User?.FindFirst("role")?.Value
                         ?? User?.FindFirst("roles")?.Value
                         ?? User?.FindFirst(RoleConstants.Admin)?.Value;

            if (Guid.TryParse(userId, out var opId) && opId != Guid.Empty && !string.IsNullOrEmpty(userName))
            {
                var role = ParseUserRole(roleStr);
                return (opId, userName, role);
            }

            _logger.LogWarning("GetOperator失败: userId={UserId}, userName={UserName}, opId={OpId}, opIdIsEmpty={OpIdIsEmpty}",
                userId, userName, opId, opId == Guid.Empty);

            throw new UnauthorizedAccessException("未登录或用户信息无效");
        }

        private UserRole ParseUserRole(string? roleStr)
        {
            if (string.IsNullOrWhiteSpace(roleStr))
            {
                _logger.LogWarning("角色值为空，默认使用Doctor");
                return UserRole.Doctor;
            }

            if (roleStr.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
            {
                roleStr = RoleConstants.SuperAdmin;
            }

            if (Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
            {
                return role;
            }

            _logger.LogWarning("无效的角色值: {RoleString}，默认使用Doctor", roleStr);
            return UserRole.Doctor;
        }

        /// <summary>
        /// 统一日志记录（带脱敏）
        /// </summary>
        protected void LogOperation(string operation, object? data = null, Guid? targetId = null)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();
                var logData = data != null ? SensitiveDataMasker.SerializeWithSanitization(data) : null;
                _logger.LogInformation(
                    "{Operation}，操作者: {OperatorName}({OperatorId}), 目标ID: {TargetId}, 数据: {Data}",
                    operation, operatorName, operatorId, targetId, logData);
            }
            catch
            {
                // 记录日志失败时不应影响主业务流程
            }
        }

        /// <summary>
        /// 获取模型验证错误
        /// </summary>
        protected List<string> GetModelErrors()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
        }

        /// <summary>
        /// 验证GUID参数
        /// </summary>
        protected bool IsValidGuid(Guid id) => id != Guid.Empty;

        /// <summary>
        /// 获取请求ID（用于链路追踪）
        /// </summary>
        protected string GetRequestId() => HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

        /// <summary>
        /// 验证模型状态
        /// </summary>
        protected bool IsModelValid => ModelState.IsValid;

        /// <summary>
        /// 获取验证错误消息
        /// </summary>
        protected string GetValidationErrorMessage() => string.Join("; ", GetModelErrors());

        #endregion

        #region API响应方法 - 统一返回IActionResult

        /// <summary>
        /// 返回成功响应（无数据）
        /// </summary>
        protected IActionResult Success(string message = "操作成功")
        {
            var response = ApiResponse.CreateSuccess(message: message);
            response.RequestId = GetRequestId();
            return Ok(response);
        }

        /// <summary>
        /// 返回成功响应（带数据）
        /// </summary>
        protected IActionResult Success<T>(T data, string message = "操作成功")
        {
            var response = ApiResponse<T>.CreateSuccess(data, message);
            response.RequestId = GetRequestId();
            return Ok(response);
        }

        /// <summary>
        /// 返回分页成功响应
        /// </summary>
        protected IActionResult SuccessPaged<T>(PagedResult<T> pagedResult, string message = "查询成功")
        {
            var items = pagedResult.Items is List<T> list ? list : pagedResult.Items.ToList();
            var pageResult = new PagedResult<T>(items, pagedResult.TotalCount, pagedResult.CurrentPage, pagedResult.PageSize);

            var response = ApiResponse<PagedResult<T>>.CreateSuccess(pageResult, message);
            response.RequestId = GetRequestId();
            return Ok(response);
        }

        /// <summary>
        /// 返回错误响应（400 Bad Request）
        /// </summary>
        protected IActionResult Error(string message)
        {
            _logger?.LogWarning("API错误: {Message}", message);
            var response = ApiResponse.CreateFail(message);
            response.RequestId = GetRequestId();
            return BadRequest(response);
        }

        /// <summary>
        /// 返回未找到响应（404 Not Found）
        /// </summary>
        protected IActionResult NotFound(string message = "资源未找到")
        {
            var response = ApiResponse.CreateFail(message);
            response.RequestId = GetRequestId();
            return base.NotFound(response);
        }

        /// <summary>
        /// 返回业务失败响应（422 Unprocessable Entity with success=false）
        /// </summary>
        protected IActionResult BusinessFail(string message, string? errorCode = null)
        {
            var response = ApiResponse.CreateFail(message);
            response.RequestId = GetRequestId();
            if (errorCode != null)
            {
                response.Errors = new { code = errorCode };
            }
            return StatusCode(422, response);
        }

        /// <summary>
        /// 返回验证失败响应（400 Bad Request）
        /// </summary>
        protected IActionResult ValidationFail(string message = "参数验证失败")
        {
            var errors = GetModelErrors();
            var response = ApiResponse.CreateFail(message, errors.Count > 0 ? errors : null);
            response.RequestId = GetRequestId();
            return BadRequest(response);
        }

        #endregion

        #region Result处理方法

        /// <summary>
        /// 处理Result返回值
        /// </summary>
        protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "操作成功")
        {
            if (result.IsSuccess)
            {
                return Success(result.Data!, successMessage);
            }
            return BusinessFail(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 处理非泛型Result返回值 - 用于Delete等无数据返回的操作
        /// </summary>
        protected IActionResult HandleResult(Result result, string successMessage = "操作成功")
        {
            if (result.IsSuccess)
            {
                return Success(successMessage);
            }
            return BusinessFail(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 处理ServiceResult返回值 - 用于同步模块的ServiceResult<T>
        /// </summary>
        protected IActionResult HandleServiceResult<T>(ServiceResult<T> result, string successMessage = "操作成功")
        {
            if (result.IsSuccess)
            {
                return Success(result.Data!, successMessage);
            }
            return BusinessFail(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 处理分页Result返回值
        /// </summary>
        protected IActionResult HandlePagedResult<T>(Result<PagedResult<T>> result, string successMessage = "查询成功")
        {
            if (result.IsSuccess)
            {
                return SuccessPaged(result.Data!, successMessage);
            }
            return BusinessFail(result.ErrorMessage ?? "查询失败");
        }

        /// <summary>
        /// 处理布尔Result返回值
        /// </summary>
        protected IActionResult HandleBoolResult(Result<bool> result, string successMessage = "操作成功")
        {
            if (result.IsSuccess)
            {
                return Success(successMessage);
            }
            return BusinessFail(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 处理带错误码的Result返回值 - 根据错误码返回正确HTTP状态码
        /// </summary>
        protected IActionResult HandleAuthResult<T>(Result<T> result, string successMessage = "操作成功")
        {
            if (result.IsSuccess)
            {
                return Success(result.Data!, successMessage);
            }

            var message = result.ErrorMessage ?? "操作失败";

            // 统一使用 ModuleErrorCode
            if (result.ModuleErrorCode.HasValue)
            {
                var moduleCode = result.ModuleErrorCode.Value;
                var httpStatus = moduleCode.ToHttpStatusCode();
                var errorResponse = CreateModuleErrorResponse<T>(message, moduleCode);

                return httpStatus switch
                {
                    401 => Unauthorized(errorResponse),
                    403 => StatusCode(403, errorResponse),
                    404 => base.NotFound(errorResponse),
                    422 => StatusCode(422, errorResponse),
                    503 => StatusCode(503, errorResponse),
                    500 => StatusCode(500, errorResponse),
                    _ => StatusCode(httpStatus, errorResponse)
                };
            }

            // 无错误码时作为业务失败处理
            return BusinessFail(message);
        }

        /// <summary>
        /// 创建统一错误码响应对象
        /// </summary>
        private ApiResponse<T> CreateModuleErrorResponse<T>(string message, GenericErrorCode errorCode)
        {
            var response = ApiResponse<T>.CreateFail(message);
            response.RequestId = GetRequestId();
            response.Errors = new { code = errorCode.ToFormattedString(), numericCode = (int)errorCode };
            return response;
        }

        // consolidate-exception-handling: HandleException方法已删除
        // 异常处理由BusinessExceptionHandler和SystemExceptionHandler统一负责

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证GUID参数，返回null表示验证通过
        /// 使用模式: if (ValidateGuid(id) is { } error) return error;
        /// </summary>
        protected IActionResult? ValidateGuid(Guid id, string paramName = "ID")
        {
            if (id == Guid.Empty)
            {
                return ValidationFail($"{paramName}不能为空");
            }
            return null;
        }

        /// <summary>
        /// 检查当前用户是否是管理员或资源所有者
        /// OpenSpec: optimize-module-list-ui - 所有权检查
        /// </summary>
        /// <param name="createdBy">资源创建者ID</param>
        /// <returns>true表示有权限（管理员或所有者），false表示无权限</returns>
        protected bool IsAdminOrOwner(Guid? createdBy)
        {
            try
            {
                var (operatorId, _, operatorRole) = GetOperator();

                // 管理员（Admin或SuperAdmin）可以操作所有资源
                if (operatorRole == UserRole.Admin || operatorRole == UserRole.SuperAdmin)
                {
                    return true;
                }

                // 非管理员需要检查所有权
                return createdBy.HasValue && createdBy.Value == operatorId;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// 验证所有权，返回null表示验证通过
        /// 使用模式: if (ValidateOwnership(createdBy) is { } error) return error;
        /// OpenSpec: optimize-module-list-ui - 所有权检查
        /// </summary>
        protected IActionResult? ValidateOwnership(Guid? createdBy, string resourceName = "资源")
        {
            if (!IsAdminOrOwner(createdBy))
            {
                _logger?.LogWarning("所有权检查失败: 用户无权操作此{ResourceName}", resourceName);
                return Forbid($"您没有权限操作此{resourceName}，只能操作自己创建的数据");
            }
            return null;
        }

        /// <summary>
        /// 返回禁止访问响应（403 Forbidden）
        /// </summary>
        protected IActionResult Forbid(string message)
        {
            var response = ApiResponse.CreateFail(message);
            response.RequestId = GetRequestId();
            return StatusCode(403, response);
        }

        /// <summary>
        /// 获取实体并验证所有权 - 重构后的统一方法
        /// 使用模式: var (dto, error) = await GetEntityWithOwnershipCheckAsync(() => _service.GetByIdAsync(id), "资源");
        ///          if (error != null) return error;
        /// OpenSpec: optimize-module-list-ui - 统一所有权检查模式
        /// </summary>
        /// <typeparam name="TDto">实现ICreatorTrackable的DTO类型</typeparam>
        /// <param name="getEntityFunc">获取实体的异步函数</param>
        /// <param name="resourceName">资源名称（用于错误消息）</param>
        /// <returns>元组：(实体数据, 错误响应)，如果error为null则表示验证通过</returns>
        protected async Task<(TDto? dto, IActionResult? error)> GetEntityWithOwnershipCheckAsync<TDto>(
            Func<Task<Result<TDto>>> getEntityFunc,
            string resourceName = "资源") where TDto : class, ICreatorTrackable
        {
            var result = await getEntityFunc();

            if (!result.IsSuccess || result.Data == null)
            {
                return (null, NotFound($"{resourceName}不存在"));
            }

            if (ValidateOwnership(result.Data.CreatedBy, resourceName) is { } ownerError)
            {
                return (null, ownerError);
            }

            return (result.Data, null);
        }

        /// <summary>
        /// 获取实体并验证所有权（使用Guid ID） - 便捷重载方法
        /// OpenSpec: optimize-module-list-ui - 统一所有权检查模式
        /// </summary>
        protected async Task<(TDto? dto, IActionResult? error)> GetEntityWithOwnershipCheckAsync<TDto>(
            Guid id,
            Func<Guid, Task<Result<TDto>>> getByIdFunc,
            string resourceName = "资源") where TDto : class, ICreatorTrackable
        {
            if (ValidateGuid(id, $"{resourceName}ID") is { } guidError)
            {
                return (null, guidError);
            }

            return await GetEntityWithOwnershipCheckAsync(() => getByIdFunc(id), resourceName);
        }

        /// <summary>
        /// 验证模型状态，返回null表示验证通过
        /// 使用模式: if (ValidateModel() is { } error) return error;
        /// </summary>
        protected IActionResult? ValidateModel()
        {
            if (!ModelState.IsValid)
            {
                return ValidationFail($"参数验证失败: {GetValidationErrorMessage()}");
            }
            return null;
        }

        /// <summary>
        /// 验证分页参数
        /// </summary>
        protected IActionResult? ValidatePagination(int page, int pageSize)
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
                return ValidationFail("分页参数无效：page 和 pageSize 必须大于 0，pageSize 不能超过 100");

            return null;
        }

        #endregion
    }
}
