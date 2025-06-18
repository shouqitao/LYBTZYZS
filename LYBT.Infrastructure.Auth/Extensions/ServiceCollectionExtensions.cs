using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LYBT.Infrastructure.Auth.Extensions {
    /// <summary>
    /// JWT 认证相关服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.AddSingleton<JwtHelper>();

            var jwt = configuration.GetSection("Jwt").Get<JwtOptions>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
                    };
                });
            services.AddAuthorization();
            return services;
        }
    }
}
