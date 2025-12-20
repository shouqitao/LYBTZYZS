using Asp.Versioning;
using LYBT.Shared.Logging.Management;
using LYBT.Infrastructure.Web;
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
[Authorize(Roles = "SuperAdmin")]  // 仅超级管理员可访问
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
    /// <returns>当前日志配置信息</returns>
    [HttpGet("logging/status")]
    public IActionResult GetLoggingStatus()
    {
        var status = _loggingLevelManager.GetStatus();
        return Ok(new
        {
            currentLevel = status.CurrentLevel,
            defaultLevel = status.DefaultLevel,
            isDebugModeActive = status.IsActive,
            debugModeStartedAt = status.StartedAt,
            debugModeExpiresAt = status.ExpiresAt,
            remainingMinutes = status.ExpiresAt.HasValue
                ? Math.Max(0, (int)(status.ExpiresAt.Value - DateTime.UtcNow).TotalMinutes)
                : (int?)null
        });
    }

    /// <summary>
    /// 启用调试模式 - 临时降低日志级别以捕获更多诊断信息
    /// </summary>
    /// <param name="request">调试模式请求参数</param>
    /// <returns>调试模式信息</returns>
    [HttpPost("logging/debug/enable")]
    public IActionResult EnableDebugMode([FromBody] EnableDebugModeRequest? request)
    {
        var (operatorId, operatorName, _) = GetOperator();

        var level = request?.Level?.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            _ => LogEventLevel.Debug
        };

        var durationMinutes = request?.DurationMinutes ?? 30;

        // 限制最大持续时间为2小时
        if (durationMinutes > 120)
        {
            durationMinutes = 120;
        }

        var result = _loggingLevelManager.EnableDebugMode(level, durationMinutes);

        _logger.LogWarning(
            "调试模式已启用 - 操作者: {OperatorName}({OperatorId}), 级别: {Level}, 持续时间: {Duration}分钟, 过期时间: {ExpiresAt}",
            operatorName, operatorId, level, durationMinutes, result.ExpiresAt);

        return Ok(new
        {
            message = "调试模式已启用",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel,
            startedAt = result.StartedAt,
            expiresAt = result.ExpiresAt,
            durationMinutes = result.DurationMinutes
        });
    }

    /// <summary>
    /// 禁用调试模式 - 恢复默认日志级别
    /// </summary>
    /// <returns>调试模式信息</returns>
    [HttpPost("logging/debug/disable")]
    public IActionResult DisableDebugMode()
    {
        var (operatorId, operatorName, _) = GetOperator();

        var result = _loggingLevelManager.DisableDebugMode();

        _logger.LogWarning(
            "调试模式已禁用 - 操作者: {OperatorName}({OperatorId}), 恢复级别: {Level}",
            operatorName, operatorId, result.CurrentLevel);

        return Ok(new
        {
            message = "调试模式已禁用，已恢复默认日志级别",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel
        });
    }

    /// <summary>
    /// 设置指定的日志级别（无自动过期）
    /// </summary>
    /// <param name="request">日志级别请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("logging/level")]
    public IActionResult SetLoggingLevel([FromBody] SetLoggingLevelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Level))
        {
            return BadRequest(new { error = "日志级别不能为空" });
        }

        if (!Enum.TryParse<LogEventLevel>(request.Level, ignoreCase: true, out var level))
        {
            return BadRequest(new
            {
                error = "无效的日志级别",
                validLevels = Enum.GetNames<LogEventLevel>()
            });
        }

        var (operatorId, operatorName, _) = GetOperator();
        var previousLevel = _loggingLevelManager.GetStatus().CurrentLevel;

        _loggingLevelManager.SetLevel(level);

        _logger.LogWarning(
            "日志级别已手动更改 - 操作者: {OperatorName}({OperatorId}), 从 {PreviousLevel} 改为 {NewLevel}",
            operatorName, operatorId, previousLevel, level);

        return Ok(new
        {
            message = "日志级别已更新",
            previousLevel,
            currentLevel = level.ToString()
        });
    }
}

/// <summary>
/// 启用调试模式请求
/// </summary>
public class EnableDebugModeRequest
{
    /// <summary>
    /// 目标日志级别（Verbose/Debug/Information，默认Debug）
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// 持续时间（分钟，默认30，最大120）
    /// </summary>
    public int? DurationMinutes { get; set; }
}

/// <summary>
/// 设置日志级别请求
/// </summary>
public class SetLoggingLevelRequest
{
    /// <summary>
    /// 目标日志级别（Verbose/Debug/Information/Warning/Error/Fatal）
    /// </summary>
    public string Level { get; set; } = string.Empty;
}
