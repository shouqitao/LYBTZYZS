using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Web;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using LYBT.LocalWebAPI.Data;
using LYBT.Shared.Logging.Management;
using LYBT.Shared.Models.Contracts.Diagnostics;
using LYBT.Shared.Models.Enums;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// Diagnostics controller: database info, version, recent logs, and logging management.
/// Read endpoints require authentication. Logging management requires Admin+.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DiagnosticsController : BaseApiController
{
    private readonly AppDbContext _db;
    private readonly LoggingLevelManager _loggingLevelManager;

    public DiagnosticsController(AppDbContext db, LoggingLevelManager loggingLevelManager, ILogger<DiagnosticsController> logger)
        : base(logger)
    {
        _db = db;
        _loggingLevelManager = loggingLevelManager;
    }

    // GET /api/diagnostics/db-info
    [HttpGet("db-info")]
    public async Task<IActionResult> GetDbInfo()
    {
        bool canConnect = false;
        try
        {
            canConnect = await _db.Database.CanConnectAsync();
        }
        catch
        {
            canConnect = false;
        }

        var providerName = _db.Database.ProviderName ?? "Unknown";

        return Success(new
        {
            provider = providerName,
            connectionState = canConnect ? "Connected" : "Disconnected",
            timestamp = DateTime.UtcNow
        });
    }

    // GET /api/diagnostics/version
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

    // GET /api/diagnostics/logs/recent?count=50
    [HttpGet("logs/recent")]
    public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 50)
    {
        if (count <= 0) count = 50;
        if (count > 500) count = 500;

        var logs = await _db.SystemLogs
            .AsNoTracking()
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .Select(l => new
            {
                l.Id,
                l.Timestamp,
                l.Level,
                l.Message,
                l.Exception,
                l.LoggerName,
                l.MachineName
            })
            .ToListAsync();

        return Success(new { count = logs.Count, items = logs });
    }

    // GET /api/diagnostics/logging/status
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

    // POST /api/diagnostics/logging/debug/enable
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

    // POST /api/diagnostics/logging/debug/disable
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

    // POST /api/diagnostics/logging/level
    [HttpPost("logging/level")]
    public IActionResult SetLoggingLevel([FromBody] SetLoggingLevelRequest request)
    {
        if (!IsAdminOrHigher()) return Forbid("仅管理员可调整日志级别");
        if (string.IsNullOrWhiteSpace(request.Level))
        {
            return Error("日志级别不能为空");
        }

        if (!Enum.TryParse<LogEventLevel>(request.Level, ignoreCase: true, out var level))
        {
            return BadRequest(new
            {
                error = "无效的日志级别",
                validLevels = Enum.GetNames<LogEventLevel>()
            });
        }

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
