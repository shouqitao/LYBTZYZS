using LYBT.Module.Auth.Models;

namespace LYBT.Module.Auth.Interfaces;

/// <summary>
/// 安全审计服务接口
/// Issue #1871: 自动记录认证相关安全事件，包含IP地址脱敏和UserAgent截断
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// 记录安全审计事件
    /// 自动从HttpContext提取IP地址和UserAgent，并进行脱敏处理
    /// </summary>
    /// <param name="auditEvent">安全审计事件</param>
    /// <returns>异步任务</returns>
    Task LogAsync(SecurityAuditEvent auditEvent);
}
