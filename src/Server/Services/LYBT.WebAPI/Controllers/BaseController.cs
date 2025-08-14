using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 基础控制器，提供通用功能
    /// </summary>
    public abstract class BaseController : ControllerBase
    {
        protected readonly ILogger _logger;
        protected readonly IMemoryCache? _cache;

        protected BaseController(ILogger logger)
        {
            _logger = logger;
        }

        protected BaseController(ILogger logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// 获取当前操作者信息
        /// </summary>
        protected (Guid operatorId, string operatorName, string operatorRole) GetOperator()
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User?.Identity?.Name;
            var roleStr = User?.FindFirst(/* ClaimTypes.Role - 字段已移除 */ "Admin")?.Value;

            if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName))
            {
                return (opId, userName, roleStr ?? "User");
            }
            throw new UnauthorizedAccessException("未登录或用户信息无效");
        }

        /// <summary>
        /// 统一的异常处理和日志记录
        /// </summary>
        protected ActionResult HandleException(Exception ex, string operation, object? context = null)
        {
            var contextInfo = context != null ? $", 上下文: {System.Text.Json.JsonSerializer.Serialize(context)}" : "";
            _logger.LogError(ex, "{Operation}失败{Context}", operation, contextInfo);
            return StatusCode(500, new ProblemDetails
            {
                Title = "系统错误",
                Detail = $"{operation}失败",
                Status = 500
            });
        }

        /// <summary>
        /// 验证模型状态
        /// </summary>
        protected ActionResult? ValidateModel()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new ProblemDetails
                {
                    Title = "参数验证失败",
                    Detail = string.Join("; ", errors),
                    Status = 400
                });
            }
            return null;
        }

        /// <summary>
        /// 验证GUID参数
        /// </summary>
        protected ActionResult? ValidateGuid(Guid id, string paramName)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "参数验证失败",
                    Detail = $"{paramName}不能为空",
                    Status = 400
                });
            }
            return null;
        }

        /// <summary>
        /// 清除缓存的帮助方法
        /// </summary>
        protected void ClearCacheByPattern(string pattern)
        {
            // 在生产环境中，可以考虑使用缓存标签或更高级的缓存清理策略
            // 目前简单实现，子类可以重写此方法提供具体的缓存清理逻辑
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        protected void LogOperation(string operation, object? data = null, Guid? targetId = null)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();
                var logData = data != null ? System.Text.Json.JsonSerializer.Serialize(data) : null;
                _logger.LogInformation("{Operation}，操作者: {OperatorName}({OperatorId}), 目标ID: {TargetId}, 数据: {Data}",
                    operation, operatorName, operatorId, targetId, logData);
            }
            catch
            {
                // 记录日志失败时不应影响主业务流程
            }
        }
    }
}