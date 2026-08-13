using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Logging.Management;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 系统诊断控制器 - 提供运行时诊断和调试功能
/// refactor-logging-system: 新增控制器，支持运行时日志级别调整
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/diagnostics")]
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
public class DiagnosticsController : BaseApiController
{
    private readonly LoggingLevelManager _loggingLevelManager;

    public DiagnosticsController(
        LoggingLevelManager loggingLevelManager,
        ILogger<DiagnosticsController> logger)
        : base(logger)
    {
        _loggingLevelManager = loggingLevelManager;
    }

    /// <summary>
    /// 获取当前日志级别状态
    /// </summary>
    [HttpGet("logging/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetLoggingStatus()
    {
        var status = _loggingLevelManager.GetStatus();
        object result = new
        {
            currentLevel = status.CurrentLevel,
            defaultLevel = status.DefaultLevel,
            isDebugModeActive = status.IsActive,
            debugModeStartedAt = status.StartedAt,
            debugModeExpiresAt = status.ExpiresAt,
            remainingMinutes = status.ExpiresAt.HasValue
                ? Math.Max(0, (int)(status.ExpiresAt.Value - DateTime.UtcNow).TotalMinutes)
                : (int?)null
        };
        return Success(result, "查询成功");
    }

    /// <summary>
    /// 启用调试模式 - 临时降低日志级别以捕获更多诊断信息
    /// </summary>
    [HttpPost("logging/debug/enable")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult EnableDebugMode([FromBody] EnableDebugModeRequest? request)
    {
        // P1-5（2026-08-14）: 枚举转换 + durationMinutes 上限移入 LoggingLevelManager——Controller 仅编排
        var result = _loggingLevelManager.EnableDebugMode(request?.Level, request?.DurationMinutes);

        object response = new
        {
            message = "调试模式已启用",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel,
            startedAt = result.StartedAt,
            expiresAt = result.ExpiresAt,
            durationMinutes = result.DurationMinutes
        };
        return Success(response, "调试模式已启用");
    }

    /// <summary>
    /// 禁用调试模式 - 恢复默认日志级别
    /// </summary>
    [HttpPost("logging/debug/disable")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult DisableDebugMode()
    {
        var result = _loggingLevelManager.DisableDebugMode();

        object response = new
        {
            message = "调试模式已禁用，已恢复默认日志级别",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel
        };
        return Success(response, "调试模式已禁用");
    }

    /// <summary>
    /// 设置指定的日志级别（无自动过期）
    /// </summary>
    [HttpPost("logging/level")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult SetLoggingLevel([FromBody] SetLoggingLevelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Level))
            return BusinessFail("日志级别不能为空");

        if (!Enum.TryParse<LogEventLevel>(request.Level, ignoreCase: true, out var level))
            return BusinessFail($"无效的日志级别，有效值: {string.Join(", ", Enum.GetNames<LogEventLevel>())}");

        var previousLevel = _loggingLevelManager.GetStatus().CurrentLevel;
        _loggingLevelManager.SetLevel(level);

        object response = new
        {
            message = "日志级别已更新",
            previousLevel,
            currentLevel = level.ToString()
        };
        return Success(response, "日志级别已更新");
    }
}
