using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.LocalWebAPI.Auth;
using LYBT.Shared.Utilities.Security;
using System.Security.Claims;
using LYBT.Entities.Users;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// Local authentication controller: login endpoint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LocalWebApiDbContext _db;

    public AuthController(LocalWebApiDbContext db)
    {
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == request.Username && !u.IsDeleted);

        if (user == null)
        {
            return Unauthorized();
        }

        // Verify password using existing helper
        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized();
        }

        // Generate JWT
        var token = LocalJwtConfig.GenerateToken(user);

        return Ok(new
        {
            Token = token,
            UserId = user.Id,
            Username = user.UserName,
            Role = user.Role
        });
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
