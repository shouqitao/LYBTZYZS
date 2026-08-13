using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using LYBT.Infrastructure.Configuration.Security;
using LYBT.Infrastructure.Web;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Configuration.Stores;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 配置控制器：持久化键值配置存储（JsonFileConfigurationStore，重启不丢，A-18 P1-6）。
/// 权限隔离（2026-08-13 修复）：配置管理 = sysadmin 专属——业务管理员（Admin）不应访问系统配置
/// （与远程端 ConfigurationController 双端一致：类级统一 SysAdminOnly）
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.SysAdminOnly)]
public class ConfigurationController : BaseApiController
{
    // SHELL-018 Phase 1: 重启限频（每小时 ≤3 次）
    private static readonly ConcurrentQueue<DateTime> RestartRequests = new();
    private static readonly TimeSpan RestartWindow = TimeSpan.FromHours(1);
    private const int RestartMaxPerHour = 3;

    private readonly IConfigurationStore _store;

    public ConfigurationController(IConfigurationStore store, ILogger<ConfigurationController> logger)
        : base(logger)
    {
        _store = store;
    }

    // GET /api/v1/configuration
    [HttpGet]
    public async Task<IActionResult> GetConfiguration(CancellationToken ct)
    {
        var items = await _store.LoadAllAsync(ct);
        return Success(new { count = items.Count, items });
    }

    // GET /api/v1/configuration/{key}
    [HttpGet("{key}")]
    public async Task<IActionResult> GetValue(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Error("Key must not be empty.");
        }

        var items = await _store.LoadAllAsync(ct);
        if (items.TryGetValue(key, out var value))
        {
            return Success(new { key, value });
        }

        return NotFound($"Key '{key}' not found.");
    }

    // PUT /api/v1/configuration/{key}
    [HttpPut("{key}")]
    public async Task<IActionResult> SetValue(string key, [FromBody] string value, CancellationToken ct)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != UserRole.SuperAdmin.ToString())
            return Forbid("仅系统管理员可修改配置");

        if (string.IsNullOrWhiteSpace(key))
        {
            return Error("Key must not be empty.");
        }

        if (value == null)
        {
            return Error("Value must not be null.");
        }

        await _store.SetValueAsync(key, value, ct);
        return Success(new { key, value });
    }

    // GET /api/v1/configuration/sections/{section}（SHELL-018 Phase 1: 敏感键脱敏；sysadmin）
    [HttpGet("sections/{section}")]
    public async Task<IActionResult> GetSection(string section, CancellationToken ct)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != UserRole.SuperAdmin.ToString())
            return Forbid("仅系统管理员可查看配置节");

        if (string.IsNullOrWhiteSpace(section))
            return Error("Section must not be empty.");

        var items = await _store.LoadAllAsync(ct);
        var prefix = $"{section}:";
        var result = items
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                kv => kv.Key[prefix.Length..],
                kv => ConfigurationWritePolicy.IsSensitive(kv.Key) ? "***" : kv.Value,
                StringComparer.OrdinalIgnoreCase);
        return Success(new { section, count = result.Count, items = result });
    }

    // PUT /api/v1/configuration/sections/{section}（SHELL-018 Phase 1: 白名单逐键 + 生效语义；sysadmin）
    [HttpPut("sections/{section}")]
    public async Task<IActionResult> UpdateSection(string section, [FromBody] Dictionary<string, string> values, CancellationToken ct)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != UserRole.SuperAdmin.ToString())
            return Forbid("仅系统管理员可修改配置");

        if (string.IsNullOrWhiteSpace(section) || values is null || values.Count == 0)
            return Error("Section and values must not be empty.");

        var fullKeys = values.ToDictionary(
            kv => $"{section}:{kv.Key}", kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        var invalid = fullKeys.Keys.Where(k => !ConfigurationWritePolicy.IsAllowed(k)).ToList();
        if (invalid.Count > 0)
            return BusinessFail($"以下配置项不在允许修改的白名单内，禁止修改: {string.Join(", ", invalid)}");

        foreach (var kv in fullKeys)
            await _store.SetValueAsync(kv.Key, kv.Value, ct);

        var isHotReload = section.Equals("FeatureToggles", StringComparison.OrdinalIgnoreCase)
            || section.Equals("ClinicSettings", StringComparison.OrdinalIgnoreCase);
        return Success(new
        {
            applied = true,
            restartRequired = !isHotReload,
            effectiveMode = isHotReload ? "hot" : "restart",
            updatedCount = fullKeys.Count
        });
    }

    // POST /api/v1/configuration/restart（SHELL-018 Phase 1: sysadmin + 限频 + 30 秒延迟）
    [HttpPost("restart")]
    public IActionResult Restart([FromServices] IHostApplicationLifetime lifetime)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != UserRole.SuperAdmin.ToString())
            return Forbid("仅系统管理员可重启服务");

        if (!TryAcquireRestartSlot())
            return BusinessFail("重启请求过于频繁（每小时最多 3 次），请稍后再试");

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
            lifetime.StopApplication();
        }, CancellationToken.None);
        return Success("重启已调度，将在 30 秒后生效（内嵌服务自动拉起）");
    }

    private static bool TryAcquireRestartSlot()
    {
        var now = DateTime.UtcNow;
        while (RestartRequests.TryPeek(out var oldest) && now - oldest > RestartWindow)
            RestartRequests.TryDequeue(out _);
        if (RestartRequests.Count >= RestartMaxPerHour)
            return false;
        RestartRequests.Enqueue(now);
        return true;
    }

    // POST /api/v1/configuration/validate
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateProduction(CancellationToken ct)
    {
        var issues = new List<string>();

        // Basic validation for local mode essentials
        var items = await _store.LoadAllAsync(ct);

        var warnings = new List<string>();
        if (items.Count == 0)
        {
            warnings.Add("No custom configuration entries found. Using defaults.");
        }

        return Success(new
        {
            valid = issues.Count == 0,
            issues,
            warnings,
            entryCount = items.Count,
            timestamp = DateTime.UtcNow
        });
    }
}
