using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web
{
    /// <summary>
    /// API控制器基类 - 简化版
    /// 提供统一的核心API响应方法，消除冗余代码
    /// </summary>
    public abstract class BaseApiController : BaseControllerCore
    {
        protected BaseApiController(ILogger<BaseApiController> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// 返回成功响应（带数据）- 保持原有兼容性
        /// </summary>
        protected ActionResult<ApiResponse> Success(string message = "操作成功")
        {
            var response = ApiResponse.CreateSuccess(message: message);
            response.RequestId = GetRequestId();
            return Ok(response);
        }

        /// <summary>
        /// 返回成功响应（带泛型数据）- 保持原有兼容性
        /// </summary>
        protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "操作成功")
        {
            var response = ApiResponse<T>.CreateSuccess(data, message);
            response.RequestId = GetRequestId();
            return Ok(response);
        }

        /// <summary>
        /// 返回分页成功响应 - 保持原有兼容性
        /// </summary>
        protected ActionResult<ApiResponse<PagedResult<T>>> Success<T>(PagedResult<T> pagedResult, string message = "查询成功")
        {
            var items = pagedResult.Items is List<T> list ? list : pagedResult.Items.ToList();
            var pageResult = new PagedResult<T>(items, pagedResult.TotalCount, pagedResult.CurrentPage, pagedResult.PageSize);

            var response = ApiResponse<PagedResult<T>>.CreateSuccess(pageResult, message);
            response.RequestId = GetRequestId();
            return Ok(response);
        }

        /// <summary>
        /// 返回错误响应
        /// </summary>
        protected ActionResult<ApiResponse> Error(string message, int code = 400)
        {
            _logger?.LogWarning("API错误: {Message}", message);
            return BadRequest(new { success = false, message, code });
        }

        /// <summary>
        /// 返回未找到响应
        /// </summary>
        protected ActionResult<ApiResponse> NotFound(string message = "资源未找到")
        {
            return NotFound(new { success = false, message, code = 404 });
        }

        /// <summary>
        /// 返回业务失败响应
        /// </summary>
        protected ActionResult<ApiResponse> BusinessFail(string message, string? errorCode = null)
        {
            return Ok(new { success = false, message, code = errorCode ?? "BUSINESS_ERROR" });
        }

        /// <summary>
        /// 返回验证失败响应
        /// </summary>
        protected ActionResult<ApiResponse> ValidationFail(string message = "参数验证失败", string? errorCode = "VALIDATION_ERROR")
        {
            return BadRequest(new { success = false, message, code = errorCode });
        }

        /// <summary>
        /// 返回错误响应（带泛型）
        /// </summary>
        protected ActionResult<ApiResponse<T>> Error<T>(string message, int code = 400)
        {
            return BadRequest(new { success = false, message, code });
        }

        /// <summary>
        /// 返回未找到响应（带泛型）
        /// </summary>
        protected ActionResult<ApiResponse<T>> NotFound<T>(string message = "资源未找到")
        {
            return NotFound(new { success = false, message, code = 404 });
        }

        /// <summary>
        /// 返回业务失败响应（带泛型）
        /// </summary>
        protected ActionResult<ApiResponse<T>> BusinessFail<T>(string message, string? errorCode = null)
        {
            return Ok(new { success = false, message, code = errorCode ?? "BUSINESS_ERROR" });
        }

        /// <summary>
        /// 返回验证失败响应（带泛型）
        /// </summary>
        protected ActionResult<ApiResponse<T>> ValidationFail<T>(string message = "参数验证失败", string? errorCode = "VALIDATION_ERROR")
        {
            return BadRequest(new { success = false, message, code = errorCode });
        }

        
        #region 兼容性Helper方法

        /// <summary>
        /// 处理分页结果 - 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse<PagedResult<T>>> HandlePagedResult<T>(ServiceResult<PagedResult<T>> result, string successMessage = "查询成功")
        {
            if (result.IsSuccess)
            {
                return Success(result.Data!, successMessage);
            }
            return Error<PagedResult<T>>(result.ErrorMessage ?? "查询失败");
        }

        /// <summary>
        /// 处理服务结果 - 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleResult<T>(ServiceResult<T> result, string successMessage = "操作成功")
        {
            if (result.IsSuccess)
            {
                return Success(result.Data!, successMessage);
            }
            return Error<T>(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 处理异常 - 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse> HandleException(Exception ex, string operation)
        {
            _logger?.LogError(ex, "{Operation}失败", operation);
            return Error($"{operation}失败: {ex.Message}");
        }

        /// <summary>
        /// 处理分页异常 - 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse> HandleExceptionPaged(Exception ex, string operation)
        {
            _logger?.LogError(ex, "{Operation}失败", operation);
            return Error($"{operation}失败: {ex.Message}");
        }

        /// <summary>
        /// 验证GUID参数 - 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse> ValidateGuid(Guid id, string paramName = "ID")
        {
            if (id == Guid.Empty)
            {
                return ValidationFail($"{paramName}不能为空");
            }
            return Success();
        }

        /// <summary>
        /// 验证GUID参数（带泛型）- 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse>? ValidateGuid<T>(Guid id, string paramName = "ID")
        {
            if (id == Guid.Empty)
            {
                return ValidationFail($"{paramName}不能为空");
            }
            return null; // 验证通过返回null
        }

        /// <summary>
        /// 验证模型 - 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse> ValidateModel()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", GetModelErrors());
                return ValidationFail($"参数验证失败: {errors}");
            }
            return Success();
        }

        /// <summary>
        /// 验证模型（带泛型）- 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse>? ValidateModel<T>()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", GetModelErrors());
                return ValidationFail($"参数验证失败: {errors}");
            }
            return null; // 验证通过返回null
        }

        /// <summary>
        /// 处理服务结果 - 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleServiceResult<T>(ServiceResult<T> result, string successMessage = "操作成功")
        {
            if (result.IsSuccess)
            {
                return Success(result.Data!, successMessage);
            }
            return BusinessFail<T>(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 获取模型错误 - 兼容旧版本方法
        /// </summary>
        protected new List<string> GetModelErrors()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
        }

        /// <summary>
        /// 验证失败响应（分页）- 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse<PagedResult<T>>> ValidationFailPaged<T>(string message = "参数验证失败", string? errorCode = "VALIDATION_ERROR")
        {
            return BadRequest(new { success = false, message, code = errorCode });
        }

        /// <summary>
        /// 未找到响应（带错误码）- 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse> NotFound(string message = "资源未找到", string? errorCode = null)
        {
            return StatusCode(404, new { success = false, message, code = errorCode ?? "NOT_FOUND" });
        }

        /// <summary>
        /// 未找到响应（带泛型和错误码）- 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse<T>> NotFound<T>(string message = "资源未找到", string? errorCode = null)
        {
            return StatusCode(404, new { success = false, message, code = errorCode ?? "NOT_FOUND" });
        }

        /// <summary>
        /// 处理分页异常（带上下文）- 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse> HandleExceptionPaged(Exception ex, string operation, object? context = null)
        {
            _logger?.LogError(ex, "{Operation}失败，上下文：{@Context}", operation, context);
            return Error($"{operation}失败: {ex.Message}");
        }

        /// <summary>
        /// 处理分页异常（带泛型和上下文）- 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleExceptionPaged<T>(Exception ex, string operation, object? context = null)
        {
            _logger?.LogError(ex, "{Operation}失败，上下文：{@Context}", operation, context);
            return Error<T>($"{operation}失败: {ex.Message}");
        }

        /// <summary>
        /// 处理异常（带上下文）- 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse> HandleException(Exception ex, string operation, object? context = null)
        {
            _logger?.LogError(ex, "{Operation}失败，上下文：{@Context}", operation, context);
            return Error($"{operation}失败: {ex.Message}");
        }

        /// <summary>
        /// 处理异常（带泛型和上下文）- 兼容旧版本方法
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleException<T>(Exception ex, string operation, object? context = null)
        {
            _logger?.LogError(ex, "{Operation}失败，上下文：{@Context}", operation, context);
            return Error<T>($"{operation}失败: {ex.Message}");
        }

        #endregion
    }

    /// <summary>
    /// 响应助手类 - 智能响应格式选择
    /// </summary>
    public static class ResponseHelper
    {
        /// <summary>
        /// 大数据直接返回（>=1KB）
        /// </summary>
        public static IActionResult DirectResponse(object? data)
        {
            return new OkObjectResult(data);
        }

        /// <summary>
        /// 小数据包装返回（<1KB）
        /// </summary>
        public static IActionResult WrappedResponse(object? data, string message = "操作成功")
        {
            return new OkObjectResult(new { success = true, message, data });
        }

        /// <summary>
        /// 自动判断包装策略
        /// </summary>
        public static IActionResult SmartResponse(object? data, string message = "操作成功")
        {
            var dataSize = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data).Length;
            return dataSize >= 1024 ? DirectResponse(data) : WrappedResponse(data, message);
        }
    }
}