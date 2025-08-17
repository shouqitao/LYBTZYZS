using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace LYBT.Infrastructure.Web
{

    /// <summary>
    /// API控制器基类 - 前后端契约统一化
    /// 提供统一的API响应格式、错误处理和业务逻辑封装
    /// </summary>
    public abstract class BaseApiController : BaseControllerCore
    {
        protected BaseApiController(ILogger logger, IMemoryCache? cache = null) 
            : base(logger, cache) { }

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
        /// 返回分页成功响应
        /// </summary>
        protected ActionResult<PagedApiResponse<T>> Success<T>(PaginatedResult<T> pagedResult, string message = "查询成功")
        {
            var response = PagedApiResponse<T>.Ok(
                pagedResult.Items, 
                pagedResult.TotalCount, 
                pagedResult.CurrentPage, 
                pagedResult.PageSize, 
                message);
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
            return base.Unauthorized(response);
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
            return base.Unauthorized(response);
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
            return base.NotFound(response);
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
            return base.NotFound(response);
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

        #endregion

        #region ServiceResult统一处理方法 - UltraThink核心模式

        /// <summary>
        /// ServiceResult自动解包并返回统一响应 - UltraThink标准模式
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleServiceResult<T>(ServiceResult<T> serviceResult, string? successMessage = null)
        {
            if (serviceResult.IsSuccess)
            {
                return Success(serviceResult.Data, successMessage ?? "操作成功");
            }
            else
            {
                return BusinessFail<T>(serviceResult.ErrorMessage ?? "操作失败");
            }
        }

        /// <summary>
        /// 分页ServiceResult自动解包
        /// </summary>
        protected ActionResult<PagedApiResponse<T>> HandlePagedServiceResult<T>(ServiceResult<PagedResult<T>> serviceResult, string? successMessage = null)
        {
            if (serviceResult.IsSuccess && serviceResult.Data != null)
            {
                var response = PagedApiResponse<T>.Ok(
                    serviceResult.Data.Items,
                    serviceResult.Data.TotalCount,
                    serviceResult.Data.CurrentPage,
                    serviceResult.Data.PageSize,
                    successMessage ?? "查询成功");
                response.RequestId = GetRequestId();
                return Ok(response);
            }
            else
            {
                var response = PagedApiResponse<T>.Fail(
                    serviceResult.ErrorMessage ?? "查询失败");
                response.RequestId = GetRequestId();
                return BadRequest(response);
            }
        }

        /// <summary>
        /// bool类型ServiceResult解包
        /// </summary>
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

        #endregion

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

        #endregion

        #region 统一异常处理

        /// <summary>
        /// 统一异常处理
        /// </summary>
        protected ActionResult<ApiResponse> HandleException(Exception ex, string operation, object? context = null)
        {
            HandleExceptionCore(ex, operation, context);
            
            // 根据异常类型返回不同的错误响应
            return ex switch
            {
                UnauthorizedAccessException => Unauthorized(ex.Message),
                ArgumentException => ValidationFail(ex.Message),
                InvalidOperationException => BusinessFail(ex.Message),
                _ => InternalError($"{operation}失败")
            };
        }

        /// <summary>
        /// 统一异常处理（泛型版本）
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleException<T>(Exception ex, string operation, object? context = null)
        {
            HandleExceptionCore(ex, operation, context);
            
            // 根据异常类型返回不同的错误响应
            return ex switch
            {
                UnauthorizedAccessException => Unauthorized<T>(ex.Message),
                ArgumentException => ValidationFail<T>(ex.Message),
                InvalidOperationException => BusinessFail<T>(ex.Message),
                _ => InternalError<T>($"{operation}失败: {ex.Message}")
            };
        }

        #endregion

        #region 分页响应专用方法

        /// <summary>
        /// 返回分页验证失败响应
        /// </summary>
        protected ActionResult<PagedApiResponse<T>> ValidationFailPaged<T>(string message = "参数验证失败", string? errorCode = "VALIDATION_ERROR")
        {
            var response = new PagedApiResponse<T>
            {
                Success = false,
                Message = message,
                RequestId = GetRequestId(),
                Data = new PagedData<T>
                {
                    Items = new List<T>(),
                    TotalCount = 0,
                    CurrentPage = 1,
                    PageSize = 10,
                    TotalPages = 0
                }
            };
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }
            return BadRequest(response);
        }

        /// <summary>
        /// 返回分页业务失败响应
        /// </summary>
        protected ActionResult<PagedApiResponse<T>> BusinessFailPaged<T>(string message, string? errorCode = null)
        {
            var response = new PagedApiResponse<T>
            {
                Success = false,
                Message = message,
                RequestId = GetRequestId(),
                Data = new PagedData<T>
                {
                    Items = new List<T>(),
                    TotalCount = 0,
                    CurrentPage = 1,
                    PageSize = 10,
                    TotalPages = 0
                }
            };
            if (!string.IsNullOrEmpty(errorCode))
            {
                response.Errors = new { code = errorCode };
            }
            return Ok(response); // 业务失败仍返回200，通过success字段区分
        }

        /// <summary>
        /// 验证分页模型状态
        /// </summary>
        protected ActionResult<PagedApiResponse<T>>? ValidateModelPaged<T>()
        {
            if (!IsModelValid)
            {
                var message = GetValidationErrorMessage();
                return ValidationFailPaged<T>(message);
            }
            return null;
        }

        /// <summary>
        /// 处理分页异常
        /// </summary>
        protected ActionResult<PagedApiResponse<T>> HandleExceptionPaged<T>(Exception ex, string operation, object? context = null)
        {
            HandleExceptionCore(ex, operation, context);
            
            var message = ex switch
            {
                UnauthorizedAccessException => ex.Message,
                ArgumentException => ex.Message,
                InvalidOperationException => ex.Message,
                _ => $"{operation}失败"
            };

            var errorCode = ex switch
            {
                UnauthorizedAccessException => "UNAUTHORIZED",
                ArgumentException => "VALIDATION_ERROR",
                InvalidOperationException => "BUSINESS_ERROR",
                _ => "INTERNAL_ERROR"
            };

            return ValidationFailPaged<T>(message, errorCode);
        }

        #endregion
    }
}