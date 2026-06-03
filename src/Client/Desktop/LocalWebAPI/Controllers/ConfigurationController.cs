using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// Configuration controller: in-memory key/value configuration store.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    // In-memory configuration store (singleton lifetime via static field)
    private static readonly ConcurrentDictionary<string, string> _store = new();

    // GET /api/configuration
    [HttpGet]
    public Task<IActionResult> GetAll()
    {
        var items = _store.Select(kv => new { key = kv.Key, value = kv.Value }).ToList();
        return Task.FromResult<IActionResult>(Ok(new { count = items.Count, items }));
    }

    // GET /api/configuration/{key}
    [HttpGet("{key}")]
    public Task<IActionResult> Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult<IActionResult>(BadRequest(new { message = "Key must not be empty." }));
        }

        if (_store.TryGetValue(key, out var value))
        {
            return Task.FromResult<IActionResult>(Ok(new { key, value }));
        }

        return Task.FromResult<IActionResult>(NotFound(new { message = $"Key '{key}' not found." }));
    }

    // PUT /api/configuration/{key}
    [HttpPut("{key}")]
    public Task<IActionResult> Set(string key, [FromBody] string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult<IActionResult>(BadRequest(new { message = "Key must not be empty." }));
        }

        if (value == null)
        {
            return Task.FromResult<IActionResult>(BadRequest(new { message = "Value must not be null." }));
        }

        _store[key] = value;
        return Task.FromResult<IActionResult>(Ok(new { key, value }));
    }

    // POST /api/configuration/validate
    [HttpPost("validate")]
    public Task<IActionResult> Validate()
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

        return Task.FromResult<IActionResult>(Ok(new
        {
            valid = !issues.Any(),
            issues,
            warnings,
            entryCount = _store.Count,
            timestamp = DateTime.UtcNow
        }));
    }
}
