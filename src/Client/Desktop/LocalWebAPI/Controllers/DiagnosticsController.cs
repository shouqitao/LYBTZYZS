using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Logging.Management;
using LYBT.Shared.Models.Contracts.Diagnostics;
using LYBT.Shared.Models.Contracts.Health;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Serilog.Events;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
public class DiagnosticsController : BaseApiController
{
    private readonly ISystemLogRepository _systemLogRepository;
    private readonly IHealthCheckService _healthCheckService;
    private readonly LoggingLevelManager _loggingLevelManager;

    public DiagnosticsController(ISystemLogRepository systemLogRepository, IHealthCheckService healthCheckService, LoggingLevelManager loggingLevelManager, ILogger<DiagnosticsController> logger)
        : base(logger)
    {
        _systemLogRepository = systemLogRepository;
        _healthCheckService = healthCheckService;
        _loggingLevelManager = loggingLevelManager;
    }

    [HttpGet("db-info")]
    public async Task<IActionResult> GetDbInfo()
    {
        var dbResult = await _healthCheckService.CheckDatabaseAsync();
        var statusString = dbResult.Status switch
        {
            HealthStatus.Healthy => "Connected",
            HealthStatus.Degraded => "Degraded",
            _ => "Disconnected"
        };

        return Success(new
        {
            provider = dbResult.Provider ?? "Unknown",
            connectionState = statusString,
            pendingMigrations = dbResult.PendingMigrationCount,
            serverVersion = dbResult.ServerVersion,
            duration = dbResult.Duration,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "Unknown";
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? version;

        return Success(new
        {
            assemblyVersion = version,
            informationalVersion,
            frameworkVersion = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("logs/recent")]
    public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 50, CancellationToken ct = default)
    {
        if (count <= 0) count = 50;
        if (count > 500) count = 500;

        var logs = await _systemLogRepository.GetRecentLogsAsync(count, ct);

        var items = logs.Select(l => new
        {
            l.Id,
            l.Timestamp,
            l.Level,
            l.Message,
            l.Exception,
            l.LoggerName,
            l.MachineName
        }).ToList();

        return Success(new { count = items.Count, items });
    }

    [HttpGet("logging/status")]
    public IActionResult GetLoggingStatus()
    {
        var status = _loggingLevelManager.GetStatus();
        return Success(new
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

    [HttpPost("logging/debug/enable")]
    public IActionResult EnableDebugMode([FromBody] EnableDebugModeRequest? request)
    {
        if (!IsAdminOrHigher()) return Forbid("仅管理员可调整日志级别");

        var level = request?.Level?.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            _ => LogEventLevel.Debug
        };

        var durationMinutes = request?.DurationMinutes ?? 30;
        if (durationMinutes > 120) durationMinutes = 120;

        var result = _loggingLevelManager.EnableDebugMode(level, durationMinutes);

        return Success(new
        {
            message = "调试模式已启用",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel,
            startedAt = result.StartedAt,
            expiresAt = result.ExpiresAt,
            durationMinutes = result.DurationMinutes
        });
    }

    [HttpPost("logging/debug/disable")]
    public IActionResult DisableDebugMode()
    {
        if (!IsAdminOrHigher()) return Forbid("仅管理员可调整日志级别");

        var result = _loggingLevelManager.DisableDebugMode();

        return Success(new
        {
            message = "调试模式已禁用，已恢复默认日志级别",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel
        });
    }

    [HttpPost("logging/level")]
    public IActionResult SetLoggingLevel([FromBody] SetLoggingLevelRequest request)
    {
        if (!IsAdminOrHigher()) return Forbid("仅管理员可调整日志级别");
        if (string.IsNullOrWhiteSpace(request.Level))
            return Error("日志级别不能为空");

        if (!Enum.TryParse<LogEventLevel>(request.Level, ignoreCase: true, out var level))
            return Error($"无效的日志级别，有效值: {string.Join(", ", Enum.GetNames<LogEventLevel>())}");

        var previousLevel = _loggingLevelManager.GetStatus().CurrentLevel;
        _loggingLevelManager.SetLevel(level);

        return Success(new
        {
            message = "日志级别已更新",
            previousLevel,
            currentLevel = level.ToString()
        });
    }

    private bool IsAdminOrHigher()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role == UserRole.Admin.ToString() || role == UserRole.SuperAdmin.ToString();
    }
}
