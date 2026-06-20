using System;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Constants;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

namespace LYBT.LocalWebAPI.Auth;

/// <summary>
/// Local JWT configuration for the embedded Web API.
/// Simplified to use a fixed HMAC-SHA256 key and 1-year expiry.
/// </summary>
public static class LocalJwtConfig
{
    private const string Secret = "LYBT-LocalWebAPI-Secret-Key-2024-DoNotUseInProduction";

    /// <summary>
    /// Configure JWT authentication/authorization services.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        var key = Encoding.UTF8.GetBytes(Secret);
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = tokenValidationParameters;
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyConstants.AdminOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin));

            options.AddPolicy(PolicyConstants.DoctorOrAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin, RoleConstants.Doctor));
        });
    }

    /// <summary>
    /// Generate a JWT for the given Identity user.
    /// Subject: user.Id, Role claim (first Identity role), 365 days expiry.
    /// </summary>
    public static string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var roleClaim = ParseRoleClaim(roles);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Role, roleClaim),
            // Include standard JWT subject claim for the user id
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };

        if (user.IsSysAdmin)
            claims.Add(new Claim("IsSysAdmin", "true"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(365),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string ParseRoleClaim(IList<string> roles)
    {
        // Identity roles seeded by IdentitySeedData ("Doctor", "Admin", "SuperAdmin", "Receptionist")
        // map 1:1 to UserRole enum names and to AuthorizationConstants policy role names.
        return roles.Count > 0 ? roles[0] : RoleConstants.Doctor;
    }
}
