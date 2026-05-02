using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// Diagnostics controller: database info, version, and recent logs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class DiagnosticsController : ControllerBase
{
    private readonly LocalWebApiDbContext _db;

    public DiagnosticsController(LocalWebApiDbContext db)
    {
        _db = db;
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

        return Ok(new
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

        return Ok(new
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

        return Ok(new { count = logs.Count, items = logs });
    }

}
