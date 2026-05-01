using System;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Entities.Users;
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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
            });

        services.AddAuthorization();
    }

    /// <summary>
    /// Generate a JWT for the given user.
    /// Subject: user.Id, Role claim, and 365 days expiry.
    /// </summary>
    public static string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            // Include standard JWT subject claim for the user id
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };

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
}
