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
        /// 返回成功响应（带数据）
        /// </summary>
        protected IActionResult Success(object? data = null, string message = "操作成功")
        {
            return ResponseHelper.WrappedResponse(data, message);
        }

        /// <summary>
        /// 返回分页成功响应
        /// </summary>
        protected IActionResult Success<T>(PagedResult<T> data, string message = "查询成功")
        {
            return ResponseHelper.WrappedResponse(new
            {
                items = data.Items,
                total = data.TotalCount,
                pageIndex = data.CurrentPage,
                pageSize = data.PageSize
            }, message);
        }

        /// <summary>
        /// 返回错误响应
        /// </summary>
        protected IActionResult Error(string message, int code = 400)
        {
            _logger?.LogWarning("API错误: {Message}", message);
            return BadRequest(new { success = false, message, code });
        }

        /// <summary>
        /// 返回未找到响应
        /// </summary>
        protected IActionResult NotFound(string message = "资源未找到")
        {
            return new NotFoundObjectResult(new { success = false, message, code = 404 });
        }

        /// <summary>
        /// 返回业务失败响应
        /// </summary>
        protected IActionResult BusinessFail(string message, string? errorCode = null)
        {
            return new OkObjectResult(new { success = false, message, code = errorCode ?? "BUSINESS_ERROR" });
        }

        /// <summary>
        /// 返回验证失败响应
        /// </summary>
        protected IActionResult ValidationFail(string message = "参数验证失败", string? errorCode = "VALIDATION_ERROR")
        {
            return BadRequest(new { success = false, message, code = errorCode });
        }
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