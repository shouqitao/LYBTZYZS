using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LYBT.Infrastructure.Utilities;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Mvc;
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

    protected BaseControllerCore(ILogger logger)
    {
        _logger = logger;
    }

    #region 核心通用功能

    /// <summary>
    /// 获取当前操作者信息 - 兼容多种Claims标准
    /// Issue #2241: 返回UserRole枚举而非字符串
    /// </summary>
    protected (Guid OperatorId, string OperatorName, UserRole OperatorRole) GetOperator()
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

        // Bug Fix: 添加 opId != Guid.Empty 检查，防止空GUID导致400错误
        // 当JWT中userId是"00000000-..."时，TryParse成功但opId为Empty
        if (Guid.TryParse(userId, out var opId) && opId != Guid.Empty && !string.IsNullOrEmpty(userName))
        {
            // Issue #2241: 将字符串角色转换为UserRole枚举
            var role = ParseUserRole(roleStr);
            return (opId, userName, role);
        }

        // 记录详细的失败原因便于调试
        _logger.LogWarning("GetOperator失败: userId={UserId}, userName={UserName}, opId={OpId}, opIdIsEmpty={OpIdIsEmpty}",
            userId, userName, opId, opId == Guid.Empty);

        throw new UnauthorizedAccessException("未登录或用户信息无效");
    }

    /// <summary>
    /// 解析用户角色字符串为UserRole枚举
    /// Issue #2241: 处理遗留命名和无效值
    /// </summary>
    private UserRole ParseUserRole(string? roleStr)
    {
        if (string.IsNullOrWhiteSpace(roleStr))
        {
            _logger.LogWarning("角色值为空，默认使用Doctor");
            return UserRole.Doctor;
        }

        // 处理遗留命名：SysAdmin → SuperAdmin
        if (roleStr.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
        {
            roleStr = "SuperAdmin";
        }

        // 尝试解析为枚举
        if (Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
        {
            return role;
        }

        // 解析失败，记录警告并使用默认值
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
