using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LYBT.Infrastructure.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// 控制器核心基类 - UltraThink统一架构标准
/// 提供所有控制器共享的核心功能，不涉及具体业务逻辑
/// 作为整个控制器体系的统一基础
/// </summary>
public abstract class BaseControllerCore : ControllerBase
{
    protected readonly ILogger _logger;
    protected readonly IMemoryCache? _cache;

    protected BaseControllerCore(ILogger logger, IMemoryCache? cache = null)
    {
        _logger = logger;
        _cache = cache;
    }

    #region 核心通用功能

    /// <summary>
    /// 获取当前操作者信息 - 兼容多种Claims标准
    /// </summary>
    protected (Guid OperatorId, string OperatorName, string OperatorRole) GetOperator()
    {
        // 尝试多种方式获取用户ID（兼容JwtRegisteredClaimNames和ClaimTypes）
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? User?.FindFirst("sub")?.Value;

        // 尝试多种方式获取用户名
        var userName = User?.Identity?.Name
                      ?? User?.FindFirst(ClaimTypes.Name)?.Value
                      ?? User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                      ?? User?.FindFirst("unique_name")?.Value
                      ?? User?.FindFirst("name")?.Value;

        // 尝试多种方式获取角色
        var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value
                     ?? User?.FindFirst("role")?.Value
                     ?? User?.FindFirst("roles")?.Value
                     ?? User?.FindFirst("Admin")?.Value;  // 兼容旧版本

        if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName))
        {
            return (opId, userName, roleStr ?? "User");
        }

        throw new UnauthorizedAccessException("未登录或用户信息无效");
    }

    /// <summary>
    /// 统一日志记录（带脱敏）
    /// </summary>
    protected void LogOperation(string operation, object? data = null, Guid? targetId = null)
    {
        try
        {
            var (operatorId, operatorName, _) = GetOperator();
            var logData = data != null ? LogSanitizer.SerializeWithSanitization(data) : null;
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
    /// 核心异常处理 - 统一日志记录（带脱敏）
    /// </summary>
    protected void HandleExceptionCore(Exception ex, string operation, object? context = null)
    {
        var sanitizedContext = context != null
            ? LogSanitizer.SerializeWithSanitization(context)
            : null;
        var contextInfo = sanitizedContext != null ? $", 上下文: {sanitizedContext}" : string.Empty;

        var sanitizedException = LogSanitizer.SanitizeException(ex);
        _logger.LogError("{Operation}失败{Context}, 错误: {Error}", operation, contextInfo, sanitizedException);
    }

    /// <summary>
    /// 基础模型验证
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
    protected bool IsValidGuid(Guid id)
    {
        return id != Guid.Empty;
    }

    /// <summary>
    /// 获取请求ID（用于链路追踪）
    /// </summary>
    protected string GetRequestId()
    {
        return HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 清除缓存的基础方法
    /// </summary>
    protected virtual void ClearCacheByPattern(string pattern)
    {
        // 基础实现，子类可以重写提供具体的缓存清理逻辑
    }

    #endregion 核心通用功能

    #region 基础验证方法

    /// <summary>
    /// 验证模型状态
    /// </summary>
    protected bool IsModelValid => ModelState.IsValid;

    /// <summary>
    /// 获取验证错误消息
    /// </summary>
    protected string GetValidationErrorMessage()
    {
        var errors = GetModelErrors();
        return string.Join("; ", errors);
    }

    #endregion 基础验证方法
}
