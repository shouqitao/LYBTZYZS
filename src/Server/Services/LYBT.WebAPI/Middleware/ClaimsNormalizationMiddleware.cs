using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LYBT.WebAPI.Middleware
{
    /// <summary>
    /// Claims标准化中间件 - 确保所有必要的Claims格式统一
    /// </summary>
    public class ClaimsNormalizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ClaimsNormalizationMiddleware> _logger;

        public ClaimsNormalizationMiddleware(
            RequestDelegate next,
            ILogger<ClaimsNormalizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var claims = new List<Claim>();
                var existingClaims = context.User.Claims.ToList();

                // 标准化用户ID Claims
                var userId = GetUserIdClaim(existingClaims);
                if (!string.IsNullOrEmpty(userId))
                {
                    // 确保所有ID格式都存在
                    EnsureClaim(claims, existingClaims, ClaimTypes.NameIdentifier, userId);
                    EnsureClaim(claims, existingClaims, JwtRegisteredClaimNames.Sub, userId);
                    EnsureClaim(claims, existingClaims, "sub", userId);
                }

                // 标准化用户名Claims
                var userName = GetUserNameClaim(existingClaims);
                if (!string.IsNullOrEmpty(userName))
                {
                    EnsureClaim(claims, existingClaims, ClaimTypes.Name, userName);
                    EnsureClaim(claims, existingClaims, JwtRegisteredClaimNames.UniqueName, userName);
                    EnsureClaim(claims, existingClaims, "unique_name", userName);
                    EnsureClaim(claims, existingClaims, "name", userName);
                }

                // 标准化角色Claims
                var role = GetRoleClaim(existingClaims);
                if (!string.IsNullOrEmpty(role))
                {
                    EnsureClaim(claims, existingClaims, ClaimTypes.Role, role);
                    EnsureClaim(claims, existingClaims, "role", role);
                    EnsureClaim(claims, existingClaims, "roles", role);
                }

                // 如果有新的claims需要添加，创建新的ClaimsPrincipal
                if (claims.Any())
                {
                    var identity = (ClaimsIdentity)context.User.Identity;
                    identity.AddClaims(claims);

                    _logger.LogDebug("标准化了 {Count} 个Claims: UserId={UserId}, UserName={UserName}, Role={Role}",
                        claims.Count, userId, userName, role);
                }
            }

            await _next(context);
        }

        #region 辅助方法

        /// <summary>
        /// 获取用户ID Claim
        /// </summary>
        private static string? GetUserIdClaim(IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == "sub")?.Value;
        }

        /// <summary>
        /// 获取用户名Claim
        /// </summary>
        private static string? GetUserNameClaim(IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Name ||
                c.Type == JwtRegisteredClaimNames.UniqueName ||
                c.Type == "unique_name" ||
                c.Type == "name")?.Value;
        }

        /// <summary>
        /// 获取角色Claim
        /// </summary>
        private static string? GetRoleClaim(IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Role ||
                c.Type == "role" ||
                c.Type == "roles")?.Value;
        }

        /// <summary>
        /// 确保Claim存在
        /// </summary>
        private static void EnsureClaim(List<Claim> newClaims, IEnumerable<Claim> existingClaims,
            string claimType, string claimValue)
        {
            if (!existingClaims.Any(c => c.Type == claimType))
            {
                newClaims.Add(new Claim(claimType, claimValue));
            }
        }

        #endregion
    }

    /// <summary>
    /// Claims标准化中间件扩展
    /// </summary>
    public static class ClaimsNormalizationMiddlewareExtensions
    {
        /// <summary>
        /// 使用Claims标准化中间件
        /// </summary>
        public static IApplicationBuilder UseClaimsNormalization(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClaimsNormalizationMiddleware>();
        }
    }
}
