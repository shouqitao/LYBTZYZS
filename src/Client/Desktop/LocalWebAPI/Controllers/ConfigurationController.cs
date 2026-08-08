using System.Security.Claims;
using System.Threading.Tasks;
using LYBT.Infrastructure.Web;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Configuration.Stores;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 配置控制器：持久化键值配置存储（JsonFileConfigurationStore，重启不丢，A-18 P1-6）。
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
public class ConfigurationController : BaseApiController
{
    private readonly IConfigurationStore _store;

    public ConfigurationController(IConfigurationStore store, ILogger<ConfigurationController> logger)
        : base(logger)
    {
        _store = store;
    }

    // GET /api/v1/configuration
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _store.LoadAllAsync(ct);
        return Success(new { count = items.Count, items });
    }

    // GET /api/v1/configuration/{key}
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
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
    public async Task<IActionResult> Set(string key, [FromBody] string value, CancellationToken ct)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != UserRole.Admin.ToString() && role != UserRole.SuperAdmin.ToString())
            return Forbid("仅管理员可修改配置");

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

    // POST /api/v1/configuration/validate
    [HttpPost("validate")]
    public async Task<IActionResult> Validate(CancellationToken ct)
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
