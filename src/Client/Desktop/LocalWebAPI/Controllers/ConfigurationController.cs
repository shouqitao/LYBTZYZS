using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// Configuration controller: in-memory key/value configuration store.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfigurationController : BaseApiController
{
    // In-memory configuration store (singleton lifetime via static field)
    private static readonly ConcurrentDictionary<string, string> _store = new();

    public ConfigurationController(ILogger<ConfigurationController> logger)
        : base(logger)
    {
    }

    // GET /api/configuration
    [HttpGet]
    public IActionResult GetAll()
    {
        var items = _store.Select(kv => new { key = kv.Key, value = kv.Value }).ToList();
        return Success(new { count = items.Count, items });
    }

    // GET /api/configuration/{key}
    [HttpGet("{key}")]
    public IActionResult Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Error("Key must not be empty.");
        }

        if (_store.TryGetValue(key, out var value))
        {
            return Success(new { key, value });
        }

        return NotFound($"Key '{key}' not found.");
    }

    // PUT /api/configuration/{key}
    [HttpPut("{key}")]
    public IActionResult Set(string key, [FromBody] string value)
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

        _store[key] = value;
        return Success(new { key, value });
    }

    // POST /api/configuration/validate
    [HttpPost("validate")]
    public IActionResult Validate()
    {
        var issues = new List<string>();

        // Basic validation for local mode essentials
        if (!_store.ContainsKey("Database:ConnectionString"))
        {
            // Connection string is configured at startup, not in store — this is OK
        }

        var warnings = new List<string>();
        if (!_store.Any())
        {
            warnings.Add("No custom configuration entries found. Using defaults.");
        }

        return Success(new
        {
            valid = !issues.Any(),
            issues,
            warnings,
            entryCount = _store.Count,
            timestamp = DateTime.UtcNow
        });
    }
}
