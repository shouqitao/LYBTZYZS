using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Authorization
{
    /// <summary>
    /// Claims 规范化处理器
    /// 负责将遗留的 User Claims 规范化为 Doctor Claims，确保向后兼容性
    /// </summary>
    public class ClaimsNormalizer
    {
        private readonly ILogger<ClaimsNormalizer> _logger;

        public ClaimsNormalizer(ILogger<ClaimsNormalizer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 规范化 ClaimsPrincipal 中的角色 Claims
        /// </summary>
        /// <param name="principal">原始 ClaimsPrincipal</param>
        /// <returns>规范化后的 ClaimsPrincipal</returns>
        public ClaimsPrincipal NormalizeClaims(ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return principal;

            var identity = (ClaimsIdentity)principal.Identity;
            var roleClaims = identity.FindAll(ClaimTypes.Role).ToList();

            if (!roleClaims.Any())
            {
                _logger.LogWarning(
                    "用户 {User} 没有角色 Claims，分配默认 Doctor 角色",
                    identity.Name ?? "Unknown");

                // 添加默认角色
                identity.AddClaim(new Claim(ClaimTypes.Role, RoleConstants.Doctor));
                return principal;
            }

            bool hasNormalization = false;

            // 处理每个角色 Claim
            foreach (var roleClaim in roleClaims.ToList())
            {
                var originalRole = roleClaim.Value;
                var normalizedRole = RoleConstants.NormalizeRole(originalRole);

                if (!string.Equals(originalRole, normalizedRole, StringComparison.OrdinalIgnoreCase))
                {
                    // 移除原始角色 Claim
                    identity.RemoveClaim(roleClaim);

                    // 添加规范化角色 Claim
                    identity.AddClaim(new Claim(ClaimTypes.Role, normalizedRole));

                    hasNormalization = true;

                    _logger.LogInformation(
                        "角色 Claims 规范化: {OriginalRole} -> {NormalizedRole} for User: {User}",
                        originalRole, normalizedRole, identity.Name ?? "Unknown");
                }
            }

            if (hasNormalization)
            {
                _logger.LogDebug(
                    "用户 {User} 的角色 Claims 已规范化完成",
                    identity.Name ?? "Unknown");
            }

            return principal;
        }

        /// <summary>
        /// 创建包含规范化角色的 Claims 列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="username">用户名</param>
        /// <param name="role">角色（支持遗留角色）</param>
        /// <param name="additionalClaims">额外的 Claims</param>
        /// <returns>规范化后的 Claims 列表</returns>
        public List<Claim> CreateNormalizedClaims(
            string userId,
            string username,
            string role,
            IDictionary<string, string>? additionalClaims = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, username),
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.UniqueName, username),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(
                    JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            // 规范化角色并添加 Claims
            var normalizedRole = RoleConstants.NormalizeRole(role);
            claims.Add(new Claim(ClaimTypes.Role, normalizedRole));

            // 如果发生了角色规范化，记录遗留角色用于审计
            if (!string.Equals(role, normalizedRole, StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("legacy_role", role));

                _logger.LogInformation(
                    "创建 JWT Claims 时角色规范化: {OriginalRole} -> {NormalizedRole} for User: {Username}",
                    role, normalizedRole, username);
            }

            // 添加额外的 Claims
            if (additionalClaims != null)
            {
                foreach (var (key, value) in additionalClaims)
                {
                    claims.Add(new Claim(key, value));
                }
            }

            return claims;
        }

        /// <summary>
        /// 从 Claims 中提取并规范化角色
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <returns>规范化后的角色</returns>
        public string ExtractNormalizedRole(ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return RoleConstants.Doctor; // 默认角色

            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
            return RoleConstants.NormalizeRole(roleClaim);
        }

        /// <summary>
        /// 检查用户是否具有指定角色（支持遗留角色检查）
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <param name="role">要检查的角色</param>
        /// <returns>是否具有该角色</returns>
        public bool HasRole(ClaimsPrincipal principal, string role)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return false;

            var userRole = ExtractNormalizedRole(principal);
            var targetRole = RoleConstants.NormalizeRole(role);

            return string.Equals(userRole, targetRole, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查用户是否具有任一指定角色
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <param name="roles">要检查的角色列表</param>
        /// <returns>是否具有任一角色</returns>
        public bool HasAnyRole(ClaimsPrincipal principal, params string[] roles)
        {
            return roles.Any(role => HasRole(principal, role));
        }
    }
}
