using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Users;
using LYBT.Shared.Utilities.Security;
using LYBT.Shared.Models.Enums; // for Role enum usage if needed
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// Minimal Users CRUD controller for LocalWebAPI.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly LocalWebApiDbContext _db;

    public UsersController(LocalWebApiDbContext db)
    {
        _db = db;
    }

    // GET /api/users
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .Select(u => new { Id = u.Id, Username = u.UserName, Role = u.Role })
            .ToListAsync();
        return Ok(users);
    }

    // GET /api/users/{id}
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<object>> GetById([FromRoute] Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();
        return Ok(new { Id = user.Id, Username = user.UserName, Role = user.Role, RealName = user.RealName, Status = user.Status });
    }

    // POST /api/users
    [HttpPost]
    [Authorize] // Admin check is enforced at runtime (not purely by Roles attribute per instructions)
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Invalid user data.");

        // Simple admin authorization check: only Admin can create new users
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (roleClaim == null || !roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var exists = await _db.Users.AnyAsync(u => u.UserName == dto.Username && !u.IsDeleted);
        if (exists) return Conflict("Username already exists.");

        var user = new User
        {
            UserName = dto.Username,
            RealName = string.IsNullOrWhiteSpace(dto.RealName) ? dto.Username : dto.RealName,
            PasswordHash = PasswordHelper.HashPassword(dto.Password, dto.Role),
            Role = dto.Role,
            Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { Id = user.Id, Username = user.UserName, Role = user.Role });
    }

    // PUT /api/users/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UserUpdateDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();

        // Admin check for update permissions
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (roleClaim == null || !roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(dto.RealName)) user.RealName = dto.RealName;
        if (!string.IsNullOrWhiteSpace(dto.Username)) user.UserName = dto.Username;
        if (dto.Role != null) user.Role = dto.Role.Value;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/users/{id} - Soft delete
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> SoftDelete([FromRoute] Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();
        user.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DTOs
    public class UserCreateDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public LYBT.Shared.Models.Enums.UserRole Role { get; set; } = LYBT.Shared.Models.Enums.UserRole.Doctor;
        public string? RealName { get; set; }
    }

    public class UserUpdateDto
    {
        public string? Username { get; set; }
        public string? RealName { get; set; }
        public LYBT.Shared.Models.Enums.UserRole? Role { get; set; }
    }
}
