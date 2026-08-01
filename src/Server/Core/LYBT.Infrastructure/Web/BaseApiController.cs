using LYBT.Shared.Logging.Masking;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode;

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
        /// 获取当前操作者信息 - 委托给 OperatorAccessor
        /// </summary>
        protected (Guid OperatorId, string OperatorName, UserRole OperatorRole) GetOperator()
        {
            var info = OperatorAccessor.GetOperator(User, _logger);
            return (info.Id, info.Name, info.Role);
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

        #endregion

        #region API响应方法 - 委托给 ControllerBaseExtensions

        protected string GetRequestId() => HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

        protected IActionResult Success(string message = "操作成功")
            => this.Success(message);

        protected IActionResult Success<T>(T data, string message = "操作成功")
            => this.Success(data, message);

        protected IActionResult SuccessPaged<T>(PagedResult<T> pagedResult, string message = "查询成功")
            => this.SuccessPaged(pagedResult, message);

        protected IActionResult Error(string message)
        {
            _logger?.LogWarning("API错误: {Message}", message);
            return this.Error(message);
        }

        protected IActionResult NotFound(string message = "资源未找到")
            => this.NotFoundResponse(message);

        protected IActionResult BusinessFail(string message, string? errorCode = null)
            => this.BusinessFail(message, errorCode);

        protected IActionResult ValidationFail(string message = "参数验证失败")
            => this.ValidationFail(message);

        protected IActionResult Forbid(string message)
            => this.ForbidResponse(message);

        #endregion

        #region Result处理方法 - 委托给 ControllerBaseExtensions

        protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "操作成功", bool useAuthMapping = false)
            => this.HandleResult(result, successMessage, useAuthMapping);

        protected IActionResult HandleResult(Result result, string successMessage = "操作成功")
            => this.HandleResult(result, successMessage);

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
        /// 验证模型状态，返回null表示验证通过
        /// 使用模式: if (ValidateModel() is { } error) return error;
        /// </summary>
        protected IActionResult? ValidateModel()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return ValidationFail($"参数验证失败: {errors}");
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


