using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web
{

    /// <summary>
    /// API控制器基类 - 前后端契约统一化
    /// 提供统一的API响应格式、错误处理和业务逻辑封装
    /// </summary>
    public abstract class BaseApiController : BaseControllerCore
    {

        protected BaseApiController(ILogger logger)
            : base(logger)
        {
        }

        #region 统一API响应包装方法

        /// <summary>
        /// 返回成功响应（带数据）
        /// </summary>
        protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "操作成功")
        {
            var response = ApiResponse<T>.CreateSuccess(data, message);
            response.RequestId = GetRequestId();
            return Ok(response);
        }

        /// <summary>
        /// 返回成功响应（无数据）
        /// </summary>
        protected ActionResult<ApiResponse> Success(string message = "操作成功")
        {
            var response = ApiResponse.CreateSuccess(message: message);
            response.RequestId = GetRequestId();
            return Ok(response);
        }

        /// <summary>
        /// 返回分页成功响应 - 统一格式：ApiResponse<PagedResult<T>>
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
        /// 返回业务失败响应
        /// </summary>
        protected ActionResult<ApiResponse<T>> BusinessFail<T>(string message, string? errorCode = null)
        {
            var response = ApiResponse<T>.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return Ok(response); // 业务失败仍返回200，通过success字段区分
        }

        /// <summary>
        /// 返回业务失败响应（无数据）
        /// </summary>
        protected ActionResult<ApiResponse> BusinessFail(string message, string? errorCode = null)
        {
            var response = ApiResponse.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return Ok(response);
        }

        /// <summary>
        /// 返回验证失败响应（400）
        /// </summary>
        protected ActionResult<ApiResponse> ValidationFail(string message = "参数验证失败", string? errorCode = "VALIDATION_ERROR")
        {
            var response = ApiResponse.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return BadRequest(response);
        }

        /// <summary>
        /// 返回验证失败响应（泛型版本）
        /// </summary>
        protected ActionResult<ApiResponse<T>> ValidationFail<T>(string message = "参数验证失败", string? errorCode = "VALIDATION_ERROR")
        {
            var response = ApiResponse<T>.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return BadRequest(response);
        }

        /// <summary>
        /// 返回未授权响应（401）
        /// </summary>
        protected ActionResult<ApiResponse> Unauthorized(string message = "未授权访问", string? errorCode = "UNAUTHORIZED")
        {
            var response = ApiResponse.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return this.Unauthorized(response);
        }

        /// <summary>
        /// 返回未授权响应（泛型版本）
        /// </summary>
        protected ActionResult<ApiResponse<T>> Unauthorized<T>(string message = "未授权访问", string? errorCode = "UNAUTHORIZED")
        {
            var response = ApiResponse<T>.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return this.Unauthorized(response);
        }

        /// <summary>
        /// 返回禁止访问响应（403）
        /// </summary>
        protected ActionResult<ApiResponse> Forbidden(string message = "禁止访问", string? errorCode = "FORBIDDEN")
        {
            var response = ApiResponse.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return StatusCode(403, response);
        }

        /// <summary>
        /// 返回资源未找到响应（404）
        /// </summary>
        protected ActionResult<ApiResponse> NotFound(string message = "资源未找到", string? errorCode = "NOT_FOUND")
        {
            var response = ApiResponse.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return this.NotFound(response);
        }

        /// <summary>
        /// 返回资源未找到响应（泛型版本）
        /// </summary>
        protected ActionResult<ApiResponse<T>> NotFound<T>(string message = "资源不存在", string? errorCode = "NOT_FOUND")
        {
            var response = ApiResponse<T>.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return this.NotFound(response);
        }

        /// <summary>
        /// 返回服务器错误响应（500）
        /// </summary>
        protected ActionResult<ApiResponse> InternalError(string message = "服务器内部错误", string? errorCode = "INTERNAL_ERROR")
        {
            var response = ApiResponse.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return StatusCode(500, response);
        }

        /// <summary>
        /// 返回服务器错误响应（泛型版本）
        /// </summary>
        protected ActionResult<ApiResponse<T>> InternalError<T>(string message = "服务器内部错误", string? errorCode = "INTERNAL_ERROR")
        {
            var response = ApiResponse<T>.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.RequestId = GetRequestId();
            return StatusCode(500, response);
        }

        #endregion 统一API响应包装方法

        #region ServiceResult统一处理方法 - UltraThink核心模式

        /// <summary>
        /// ServiceResult自动解包并返回统一响应 - UltraThink标准模式
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleServiceResult<T>(ServiceResult<T> serviceResult, string? successMessage = null)
        {
            if (serviceResult.IsSuccess)
            {
                return Success(serviceResult.Data!, successMessage ?? "操作成功");
            }
            else
            {
                return BusinessFail<T>(serviceResult.ErrorMessage ?? "操作失败");
            }
        }

        /// <summary>
        /// 分页ServiceResult自动解包 - 统一格式：ApiResponse<PagedResult<T>>
        /// </summary>
        protected ActionResult<ApiResponse<PagedResult<T>>> HandlePagedServiceResult<T>(ServiceResult<PagedResult<T>> serviceResult, string? successMessage = null)
        {
            if (serviceResult.IsSuccess && serviceResult.Data != null)
            {
                var response = ApiResponse<PagedResult<T>>.CreateSuccess(serviceResult.Data, successMessage ?? "查询成功");
                response.RequestId = GetRequestId();
                return Ok(response);
            }
            else
            {
                var response = ApiResponse<PagedResult<T>>.CreateFail(serviceResult.ErrorMessage ?? "查询失败");
                response.RequestId = GetRequestId();
                return BadRequest(response);
            }
        }

        /// <summary>
        /// 非泛型ServiceResult解包（无数据返回场景，如删除操作）
        /// </summary>
        protected ActionResult<ApiResponse> HandleServiceResult(ServiceResult serviceResult, string? successMessage = null)
        {
            if (serviceResult.IsSuccess)
            {
                return Success(successMessage ?? "操作成功");
            }
            else
            {
                return BusinessFail(serviceResult.ErrorMessage ?? "操作失败");
            }
        }

        protected ActionResult<ApiResponse> HandleBoolServiceResult(ServiceResult<bool> serviceResult, string? successMessage = null, string? failMessage = null)
        {
            if (serviceResult.IsSuccess && serviceResult.Data)
            {
                return Success(successMessage ?? "操作成功");
            }
            else
            {
                return BusinessFail(failMessage ?? serviceResult.ErrorMessage ?? "操作失败");
            }
        }

        #endregion ServiceResult统一处理方法 - UltraThink核心模式

        #region Result<T>统一处理方法 - Phase 1 Task 1.6

        /// <summary>
        /// Result<T>自动解包并返回统一响应
        /// Phase 1 Task 1.6: 新的Result<T>返回值模式
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleResult<T>(LYBT.Shared.Models.Common.Result<T> result, string? successMessage = null)
        {
            if (result.IsSuccess)
            {
                return Success(result.Data!, successMessage ?? "操作成功");
            }
            else
            {
                return BusinessFail<T>(result.ErrorMessage ?? "操作失败");
            }
        }

        /// <summary>
        /// 分页Result<T>自动解包 - 统一格式：ApiResponse<PagedResult<T>>
        /// Phase 1 Task 1.6: 新的Result<T>返回值模式
        /// </summary>
        protected ActionResult<ApiResponse<PagedResult<T>>> HandlePagedResult<T>(LYBT.Shared.Models.Common.Result<PagedResult<T>> result, string? successMessage = null)
        {
            if (result.IsSuccess && result.Data != null)
            {
                var response = ApiResponse<PagedResult<T>>.CreateSuccess(result.Data, successMessage ?? "查询成功");
                response.RequestId = GetRequestId();
                return Ok(response);
            }
            else
            {
                var response = ApiResponse<PagedResult<T>>.CreateFail(result.ErrorMessage ?? "查询失败");
                response.RequestId = GetRequestId();
                return BadRequest(response);
            }
        }

        /// <summary>
        /// 非泛型Result解包（无数据返回场景，如删除操作）
        /// Phase 1 Task 1.6: 新的Result返回值模式
        /// </summary>
        protected ActionResult<ApiResponse> HandleResult(LYBT.Shared.Models.Common.Result result, string? successMessage = null)
        {
            if (result.IsSuccess)
            {
                return Success(successMessage ?? "操作成功");
            }
            else
            {
                return BusinessFail(result.ErrorMessage ?? "操作失败");
            }
        }

        #endregion Result<T>统一处理方法 - Phase 1 Task 1.6

        #region 业务验证方法

        /// <summary>
        /// 验证模型状态
        /// </summary>
        protected ActionResult<ApiResponse>? ValidateModel()
        {
            if (!IsModelValid)
            {
                var message = GetValidationErrorMessage();
                return ValidationFail(message);
            }

            return null;
        }

        /// <summary>
        /// 验证模型状态（泛型版本）
        /// </summary>
        protected ActionResult<ApiResponse<T>>? ValidateModel<T>()
        {
            if (!IsModelValid)
            {
                var message = GetValidationErrorMessage();
                return ValidationFail<T>(message);
            }

            return null;
        }

        /// <summary>
        /// 验证GUID参数
        /// </summary>
        protected ActionResult<ApiResponse>? ValidateGuid(Guid id, string paramName)
        {
            if (!IsValidGuid(id))
            {
                return ValidationFail($"{paramName}不能为空");
            }

            return null;
        }

        /// <summary>
        /// 验证GUID参数（泛型版本）
        /// </summary>
        protected ActionResult<ApiResponse<T>>? ValidateGuid<T>(Guid id, string paramName)
        {
            if (!IsValidGuid(id))
            {
                return ValidationFail<T>($"{paramName}不能为空");
            }

            return null;
        }

        #endregion 业务验证方法

        #region 统一异常处理

        /// <summary>
        /// 统一异常处理 - 转换为标准异常类型供GlobalExceptionHandler处理
        /// </summary>
        protected ActionResult<ApiResponse> HandleException(Exception ex, string operation, object? context = null)
        {
            // 记录带脱敏的异常信息，但不传递敏感的context到异常
            HandleExceptionCore(ex, operation, context);

            // 转换为标准异常类型，让GlobalExceptionHandler统一处理ProblemDetails响应
            // 注意：不再将context信息包含在抛出的异常中
            switch (ex)
            {
                case UnauthorizedAccessException:
                    throw ex; // 保持原异常类型
                case ArgumentException:
                    throw new ValidationException(ex.Message, ex);
                case InvalidOperationException:
                    throw new BusinessException(ex.Message, ex);
                default:
                    throw new AppException($"{operation}失败", ex);
            }
        }

        /// <summary>
        /// 统一异常处理（泛型版本）- 转换为标准异常类型供GlobalExceptionHandler处理
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleException<T>(Exception ex, string operation, object? context = null)
        {
            // 记录带脱敏的异常信息，但不传递敏感的context到异常
            HandleExceptionCore(ex, operation, context);

            // 转换为标准异常类型，让GlobalExceptionHandler统一处理ProblemDetails响应
            // 注意：不再将context信息包含在抛出的异常中
            switch (ex)
            {
                case UnauthorizedAccessException:
                    throw ex; // 保持原异常类型
                case ArgumentException:
                    throw new ValidationException(ex.Message, ex);
                case InvalidOperationException:
                    throw new BusinessException(ex.Message, ex);
                default:
                    throw new AppException($"{operation}失败", ex);
            }
        }

        #endregion 统一异常处理

        #region 分页响应专用方法

        /// <summary>
        /// 返回分页验证失败响应 - 统一格式：ApiResponse<PagedResult<T>>
        /// </summary>
        protected ActionResult<ApiResponse<PagedResult<T>>> ValidationFailPaged<T>(string message = "参数验证失败", string? errorCode = "VALIDATION_ERROR")
        {
            var pagedResult = new PagedResult<T>(new List<T>(), 0, 1, 10);

            var response = ApiResponse<PagedResult<T>>.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.Data = pagedResult;
            response.RequestId = GetRequestId();
            return BadRequest(response);
        }

        /// <summary>
        /// 返回分页业务失败响应 - 统一格式：ApiResponse<PagedResult<T>>
        /// </summary>
        protected ActionResult<ApiResponse<PagedResult<T>>> BusinessFailPaged<T>(string message, string? errorCode = null)
        {
            var pagedResult = new PagedResult<T>(new List<T>(), 0, 1, 10);

            var response = ApiResponse<PagedResult<T>>.CreateFail(message);
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }

            response.Data = pagedResult;
            response.RequestId = GetRequestId();
            return Ok(response); // 业务失败仍返回200，通过success字段区分
        }

        /// <summary>
        /// 验证分页模型状态 - 统一格式：ApiResponse<PagedResult<T>>
        /// </summary>
        protected ActionResult<ApiResponse<PagedResult<T>>>? ValidateModelPaged<T>()
        {
            if (!IsModelValid)
            {
                var message = GetValidationErrorMessage();
                return ValidationFailPaged<T>(message);
            }

            return null;
        }

        /// <summary>
        /// 处理分页异常 - 转换为标准异常类型供GlobalExceptionHandler处理
        /// </summary>
        protected ActionResult<ApiResponse<PagedResult<T>>> HandleExceptionPaged<T>(Exception ex, string operation, object? context = null)
        {
            // 记录带脱敏的异常信息，但不传递敏感的context到异常
            HandleExceptionCore(ex, operation, context);

            // 转换为标准异常类型，让GlobalExceptionHandler统一处理ProblemDetails响应
            // 注意：不再将context信息包含在抛出的异常中
            switch (ex)
            {
                case UnauthorizedAccessException:
                    throw ex; // 保持原异常类型
                case ArgumentException:
                    throw new ValidationException(ex.Message, ex);
                case InvalidOperationException:
                    throw new BusinessException(ex.Message, ex);
                default:
                    throw new AppException($"{operation}失败", ex);
            }
        }

        #endregion 分页响应专用方法
    }
}
