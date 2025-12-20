using System.Diagnostics;

namespace LYBT.Shared.Logging;

/// <summary>
/// 分布式追踪上下文辅助类
/// 基于System.Diagnostics.Activity API，提供统一的TraceId访问
/// </summary>
/// <remarks>
/// Activity API优势：
/// - .NET原生支持，自动AsyncLocal传播
/// - HttpClient自动添加W3C traceparent头
/// - 兼容OpenTelemetry标准
/// </remarks>
public static class TraceContext
{
    /// <summary>
    /// 获取当前TraceId（可能为null）
    /// </summary>
    public static string? CurrentTraceId => Activity.Current?.TraceId.ToString();

    /// <summary>
    /// 获取当前TraceId，如果不存在则生成新的Guid
    /// </summary>
    public static string TraceIdOrNew =>
        Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

    /// <summary>
    /// 获取当前SpanId（可能为null）
    /// </summary>
    public static string? CurrentSpanId => Activity.Current?.SpanId.ToString();

    /// <summary>
    /// 启动新的Activity（用于追踪操作）
    /// </summary>
    /// <param name="operationName">操作名称</param>
    /// <returns>新启动的Activity，使用using语句自动结束</returns>
    public static Activity? StartActivity(string operationName)
    {
        return new Activity(operationName).Start();
    }

    /// <summary>
    /// 检查当前是否有活动的追踪上下文
    /// </summary>
    public static bool HasActiveTrace => Activity.Current != null;
}
