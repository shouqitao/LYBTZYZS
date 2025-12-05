using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LYBT.Infrastructure.Logging;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
                         ?? User?.FindFirst("Admin")?.Value;

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
                roleStr = "SuperAdmin";
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
        /// 返回业务失败响应（200 OK with success=false）
        /// </summary>
        protected IActionResult BusinessFail(string message, string? errorCode = null)
        {
            var response = ApiResponse.CreateFail(message);
            response.RequestId = GetRequestId();
            if (errorCode != null)
            {
                response.Errors = new { code = errorCode };
            }
            return Ok(response);
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
        /// 处理异常
        /// </summary>
        protected IActionResult HandleException(Exception ex, string operation, object? context = null)
        {
            if (context != null)
            {
                var sanitizedContext = SensitiveDataMasker.SerializeWithSanitization(context);
                _logger?.LogError(ex, "{Operation}失败，上下文：{Context}", operation, sanitizedContext);
            }
            else
            {
                _logger?.LogError(ex, "{Operation}失败", operation);
            }
            return Error($"{operation}失败: {ex.Message}");
        }

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

        #endregion
    }
}
