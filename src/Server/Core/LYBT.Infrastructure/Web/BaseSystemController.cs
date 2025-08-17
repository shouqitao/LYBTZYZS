using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// 系统管理控制器基类 - UltraThink架构标准
/// 专门用于系统级功能：监控、缓存、性能、健康检查、安全管理等
/// 提供简化的响应格式，不需要复杂的ServiceResult处理
/// </summary>
public abstract class BaseSystemController : BaseControllerCore
{
    protected BaseSystemController(ILogger logger, IMemoryCache? cache = null)
        : base(logger, cache) { }

    #region 系统级响应方法

    /// <summary>
    /// 系统状态响应 (简化格式)
    /// </summary>
    protected IActionResult SystemOk(object data, string message = "系统正常")
    {
        var response = new
        {
            success = true,
            message,
            data,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            requestId = GetRequestId()
        };
        return Ok(response);
    }

    /// <summary>
    /// 系统状态响应 (无数据)
    /// </summary>
    protected IActionResult SystemOk(string message = "系统正常")
    {
        var response = new
        {
            success = true,
            message,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            requestId = GetRequestId()
        };
        return Ok(response);
    }

    /// <summary>
    /// 系统错误响应
    /// </summary>
    protected IActionResult SystemError(string message, int statusCode = 500)
    {
        var response = new
        {
            success = false,
            message,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            requestId = GetRequestId()
        };
        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// 系统警告响应 (业务正常但有警告)
    /// </summary>
    protected IActionResult SystemWarning(object data, string message)
    {
        var response = new
        {
            success = true,
            warning = true,
            message,
            data,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            requestId = GetRequestId()
        };
        return Ok(response);
    }

    #endregion

    #region 系统专用验证方法

    /// <summary>
    /// 验证系统管理员权限
    /// </summary>
    protected bool IsSystemAdmin()
    {
        try
        {
            var (_, _, role) = GetOperator();
            return role?.Contains("Admin") == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证参数并返回错误响应
    /// </summary>
    protected IActionResult? ValidateSystemParameters(params (bool condition, string message)[] validations)
    {
        foreach (var (condition, message) in validations)
        {
            if (!condition)
            {
                return SystemError(message, 400);
            }
        }
        return null;
    }

    #endregion

    #region 系统异常处理

    /// <summary>
    /// 系统级异常处理
    /// </summary>
    protected IActionResult HandleSystemException(Exception ex, string operation, object? context = null)
    {
        HandleExceptionCore(ex, operation, context);

        // 系统级异常不暴露详细信息
        var message = ex switch
        {
            UnauthorizedAccessException => "访问被拒绝",
            ArgumentException => "参数错误",
            InvalidOperationException => "操作无效",
            _ => $"{operation}执行失败"
        };

        var statusCode = ex switch
        {
            UnauthorizedAccessException => 401,
            ArgumentException => 400,
            InvalidOperationException => 409,
            _ => 500
        };

        return SystemError(message, statusCode);
    }

    #endregion

    #region 缓存操作增强

    /// <summary>
    /// 清除缓存并记录操作
    /// </summary>
    protected override void ClearCacheByPattern(string pattern)
    {
        try
        {
            // 这里可以实现具体的缓存清理逻辑
            // 例如使用 IMemoryCache 或 Redis 的模式匹配删除
            
            LogOperation($"清除缓存", new { pattern }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除缓存失败: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    protected object GetCacheStats()
    {
        // 返回缓存统计信息，具体实现根据使用的缓存类型决定
        return new
        {
            cacheProvider = _cache?.GetType().Name ?? "None",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    #endregion

    #region 系统监控辅助方法

    /// <summary>
    /// 获取系统基础信息
    /// </summary>
    protected object GetSystemInfo()
    {
        return new
        {
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown",
            framework = Environment.Version.ToString(),
            platform = Environment.OSVersion.VersionString,
            serverTime = DateTimeOffset.UtcNow,
            processId = Environment.ProcessId,
            workingSet = GC.GetTotalMemory(false)
        };
    }

    /// <summary>
    /// 检查系统健康状态
    /// </summary>
    protected object GetHealthStatus()
    {
        try
        {
            var memoryUsage = GC.GetTotalMemory(false);
            var isHealthy = memoryUsage < 500 * 1024 * 1024; // 简单的内存检查，500MB阈值

            return new
            {
                status = isHealthy ? "Healthy" : "Warning",
                checks = new
                {
                    memory = new
                    {
                        status = isHealthy ? "Healthy" : "Warning",
                        usage = memoryUsage,
                        threshold = 500 * 1024 * 1024
                    },
                    timestamp = DateTimeOffset.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return new
            {
                status = "Unhealthy",
                error = "健康检查执行失败",
                timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    #endregion
}