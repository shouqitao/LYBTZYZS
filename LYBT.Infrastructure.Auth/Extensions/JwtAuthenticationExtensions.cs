using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LYBT.Infrastructure.Auth.Extensions {
    /// <summary>
    /// JWT 认证服务扩展
    /// </summary>
    public static class JwtAuthenticationExtensions {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration) {
            var jwtSection = configuration.GetSection("JwtOptions");
            services.Configure<JwtOptions>(jwtSection);

            var options = jwtSection.Get<JwtOptions>();

            services.AddAuthentication(options2 => {
                options2.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options2.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts => {
                opts.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret))
                };
            });

            return services;
        }
    }
}
