using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// 安全审计服务实现
/// Issue #1871: 自动记录认证相关安全事件，包含IP地址脱敏和UserAgent截断
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecurityAuditService(
        AppDbContext context,
        ILogger<SecurityAuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 记录安全审计事件
    /// </summary>
    public async Task LogAsync(SecurityAuditEvent auditEvent)
    {
        try
        {
            // 从HttpContext提取信息
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = ExtractAndMaskIpAddress(httpContext);
            var userAgent = ExtractAndTruncateUserAgent(httpContext);

            // 创建审计日志实体
            var auditLog = new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                EventType = auditEvent.EventType,
                UserId = auditEvent.UserId,
                UserType = auditEvent.UserType,
                UserName = auditEvent.UserName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = auditEvent.Success,
                ErrorMessage = auditEvent.ErrorMessage,
                Metadata = auditEvent.Metadata,
                CreatedAt = DateTime.UtcNow
            };

            // 异步记录到数据库
            await _context.SecurityAuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Security audit logged: EventType={EventType}, UserId={UserId}, Success={Success}",
                auditEvent.EventType, auditEvent.UserId, auditEvent.Success);
        }
        catch (Exception ex)
        {
            // 审计日志失败不影响主流程，只记录错误日志
            _logger.LogError(ex,
                "Failed to log security audit event: EventType={EventType}, UserId={UserId}",
                auditEvent.EventType, auditEvent.UserId);
        }
    }

    /// <summary>
    /// 提取并脱敏IP地址
    /// 示例：192.168.1.100 → 192.168.1.*
    /// </summary>
    private string? ExtractAndMaskIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipAddress))
            return null;

        // IPv4地址脱敏：保留前三段，最后一段替换为*
        var parts = ipAddress.Split('.');
        if (parts.Length == 4)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}.*";
        }

        // IPv6地址脱敏：保留前4组，其余替换为*
        var ipv6Parts = ipAddress.Split(':');
        if (ipv6Parts.Length > 4)
        {
            return $"{ipv6Parts[0]}:{ipv6Parts[1]}:{ipv6Parts[2]}:{ipv6Parts[3]}:*";
        }

        // 其他格式直接返回（例如localhost）
        return ipAddress;
    }

    /// <summary>
    /// 提取并截断UserAgent
    /// 最大长度：500字符
    /// </summary>
    private string? ExtractAndTruncateUserAgent(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        if (string.IsNullOrEmpty(userAgent))
            return null;

        // 截断到最大500字符
        const int maxLength = 500;
        return userAgent.Length > maxLength
            ? userAgent[..maxLength]
            : userAgent;
    }
}
