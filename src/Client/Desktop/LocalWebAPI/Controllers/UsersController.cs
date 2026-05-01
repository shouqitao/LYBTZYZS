using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Utilities.Security;
using LYBT.Shared.Models.Enums; // for Role enum usage if needed
using LYBT.Shared.Models.Contracts.Users;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

using LYBT.LocalWebAPI.Mappers;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// Minimal Users CRUD controller for LocalWebAPI.
/// </summary>
[Authorize]
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
        public async Task<ActionResult<object>> GetById([FromRoute] Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();
        return Ok(new { Id = user.Id, Username = user.UserName, Role = user.Role, RealName = user.RealName, Status = user.Status });
    }

    // POST /api/users
        [HttpPost]
        // Admin check is enforced at runtime (not purely by Roles attribute per instructions)
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
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();
        user.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PUT /api/users/{id}/change-password
    [HttpPut("{id:guid}/change-password")]
    public async Task<IActionResult> ChangePassword([FromRoute] Guid id, [FromBody] ChangePasswordDto dto)
    {
        if (dto == null) return BadRequest("Invalid request.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();

        if (!PasswordHelper.VerifyPassword(dto.OldPassword, user.PasswordHash, user.Role).IsSuccess)
            return BadRequest("Old password is incorrect.");

        user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword, user.Role);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/users/{id}/toggle-status
    [HttpPost("{id}/toggle-status")]
    public async Task<ActionResult<object>> ToggleStatus([FromRoute] Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();

        user.Status = user.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
        await _db.SaveChangesAsync();
        return Ok(new { Id = user.Id, Username = user.UserName, Role = user.Role, Status = user.Status });
    }

    // POST /api/users/{id}/restore
    [HttpPost("{id}/restore")]
    public async Task<ActionResult<object>> Restore([FromRoute] Guid id)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted);
        if (user == null) return NotFound();

        user.IsDeleted = false;
        await _db.SaveChangesAsync();
        return Ok(new { Id = user.Id, Username = user.UserName, Role = user.Role, Status = user.Status });
    }

    // POST /api/users/batch-delete
    [HttpPost("batch-delete")]
    public async Task<ActionResult<object>> BatchDelete([FromBody] List<Guid> ids)
    {
        if (ids == null || ids.Count == 0) return BadRequest("No ids provided.");

        var users = await _db.Users.Where(u => ids.Contains(u.Id) && !u.IsDeleted).ToListAsync();
        foreach (var u in users) u.IsDeleted = true;
        await _db.SaveChangesAsync();
        return Ok(new { Count = users.Count });
    }

    // POST /api/users/batch-enable
    [HttpPost("batch-enable")]
    public async Task<ActionResult<object>> BatchEnable([FromBody] List<Guid> ids)
    {
        if (ids == null || ids.Count == 0) return BadRequest("No ids provided.");

        var users = await _db.Users.Where(u => ids.Contains(u.Id) && !u.IsDeleted).ToListAsync();
        foreach (var u in users) u.Status = CommonStatus.Enabled;
        await _db.SaveChangesAsync();
        return Ok(new { Count = users.Count });
    }

    // POST /api/users/batch-disable
    [HttpPost("batch-disable")]
    public async Task<ActionResult<object>> BatchDisable([FromBody] List<Guid> ids)
    {
        if (ids == null || ids.Count == 0) return BadRequest("No ids provided.");

        var users = await _db.Users.Where(u => ids.Contains(u.Id) && !u.IsDeleted).ToListAsync();
        foreach (var u in users) u.Status = CommonStatus.Disabled;
        await _db.SaveChangesAsync();
        return Ok(new { Count = users.Count });
    }

    // GET /api/users/current
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("无法获取当前用户信息。");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null) return NotFound("用户不存在。");

        return Ok(new UserDetailDto
        {
            Id = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Role = user.Role,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    // POST /api/users/{id}/reset-password
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword([FromRoute] Guid id)
    {
        // Admin check
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (roleClaim == null || !roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();

        var tempPassword = PasswordHelper.GenerateTemporaryPassword();
        user.PasswordHash = PasswordHelper.HashPassword(tempPassword, user.Role);
        user.MustChangeOnNextLogin = true;
        await _db.SaveChangesAsync();

        return Ok(new ResetPasswordResponseDto
        {
            Success = true,
            TemporaryPassword = tempPassword
        });
    }

    // PUT /api/users/{id}/profile
    [HttpPut("{id:guid}/profile")]
    public async Task<IActionResult> ChangeProfile([FromRoute] Guid id, [FromBody] ChangeProfileDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();

        user.RealName = dto.RealName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Email = dto.Email;
        await _db.SaveChangesAsync();

        return Ok(new UserDetailDto
        {
            Id = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Role = user.Role,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    // DTOs
    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

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
