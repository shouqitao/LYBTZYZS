using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace LYBT.WPF.Client.Core.Logging
{
    /// <summary>
    /// 结构化日志服务接口
    /// </summary>
    public interface IStructuredLoggingService
    {
        // 基础日志方法
        void LogTrace(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
        void LogCritical(Exception exception, string message, params object[] args);
        
        // 结构化日志方法
        void LogOperation(string operationName, object parameters = null,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFile = "",
            [CallerLineNumber] int callerLine = 0);
            
        IDisposable BeginPerformanceLog(string operationName);
        
        void LogAudit(string action, string entityType, object entityId,
            object oldValue = null, object newValue = null);
            
        void LogSecurity(string eventType, string description,
            SecurityEventSeverity severity = SecurityEventSeverity.Medium);
            
        void LogBusinessEvent(string eventName, object eventData = null);
    }
    
    /// <summary>
    /// 日志上下文提供者接口
    /// </summary>
    public interface ILogContextProvider
    {
        LogContext GetCurrentContext();
        string GetCorrelationId();
        string GetSessionId();
        string GetCurrentUserId();
        string GetCurrentUserName();
        string GetClientIpAddress();
        void SetContextProperty(string key, object value);
        void ClearContextProperty(string key);
    }
}