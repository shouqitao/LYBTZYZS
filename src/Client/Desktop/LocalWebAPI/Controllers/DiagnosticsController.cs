using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Web;
using LYBT.LocalWebAPI.Commands;
using LYBT.Shared.Models.Contracts.Diagnostics;
using LYBT.Shared.Models.Contracts.Health;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DiagnosticsController : BaseApiController
{
    private readonly AppDbContext _db;
    private readonly IHealthCheckService _healthCheckService;
    private readonly ISender _sender;

    public DiagnosticsController(AppDbContext db, IHealthCheckService healthCheckService, ISender sender, ILogger<DiagnosticsController> logger)
        : base(logger)
    {
        _db = db;
        _healthCheckService = healthCheckService;
        _sender = sender;
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
    // TODO: 注入 ISystemLogRepository 替代直接查询 AppDbContext（目前未注册）
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

    [HttpGet("logging/status")]
    public async Task<IActionResult> GetLoggingStatus(CancellationToken ct)
    {
        var result = await _sender.Send(new GetLoggingStatusQuery(), ct);
        return Ok(result);
    }

    [HttpPost("logging/debug/enable")]
    public async Task<IActionResult> EnableDebugMode([FromBody] EnableDebugModeRequest? request, CancellationToken ct)
    {
        if (!IsAdminOrHigher()) return Forbid("仅管理员可调整日志级别");
        var result = await _sender.Send(new EnableDebugModeCommand(request?.Level, request?.DurationMinutes), ct);
        return Ok(result);
    }

    [HttpPost("logging/debug/disable")]
    public async Task<IActionResult> DisableDebugMode(CancellationToken ct)
    {
        if (!IsAdminOrHigher()) return Forbid("仅管理员可调整日志级别");
        var result = await _sender.Send(new DisableDebugModeCommand(), ct);
        return Ok(result);
    }

    [HttpPost("logging/level")]
    public async Task<IActionResult> SetLoggingLevel([FromBody] SetLoggingLevelRequest request, CancellationToken ct)
    {
        if (!IsAdminOrHigher()) return Forbid("仅管理员可调整日志级别");
        if (string.IsNullOrWhiteSpace(request.Level))
            return Error("日志级别不能为空");

        var result = await _sender.Send(new SetLoggingLevelCommand(request.Level), ct);
        if (!result.Success)
            return ValidationFail(result.Message);
        return Ok(result);
    }

    private bool IsAdminOrHigher()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role == UserRole.Admin.ToString() || role == UserRole.SuperAdmin.ToString();
    }
}
