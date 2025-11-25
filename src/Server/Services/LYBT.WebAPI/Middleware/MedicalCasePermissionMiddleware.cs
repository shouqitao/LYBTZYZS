using System.Security.Claims;
using LYBT.Shared.Models.Enums;

namespace LYBT.WebAPI.Middleware
{
    /// <summary>
    /// MedicalCase权限验证中间件
    /// 统一处理MedicalCase相关端点的权限验证逻辑
    /// Epic #1612: MedicalCase模块权限优化
    /// </summary>
    public class MedicalCasePermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MedicalCasePermissionMiddleware> _logger;

        // MedicalCase相关端点路径模式
        private static readonly string[] MedicalCasePaths =
        {
            "/api/v1/medicalcases",
            "/api/v2/medicalcases"  // 为未来版本预留
        };

        // 需要权限验证的HTTP方法
        private static readonly string[] RestrictedMethods =
        {
            "PUT", "PATCH", "DELETE"
        };

        public MedicalCasePermissionMiddleware(
            RequestDelegate next,
            ILogger<MedicalCasePermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 检查是否为MedicalCase相关请求
            if (IsMedicalCaseRequest(context.Request))
            {
                // 提取并验证用户权限信息
                var userInfo = ExtractUserInfo(context);
                if (userInfo != null)
                {
                    // 将权限信息添加到HttpContext.Items中供后续使用
                    context.Items["MedicalCaseUserInfo"] = userInfo;

                    _logger.LogDebug("MedicalCase权限验证通过: UserId={UserId}, UserName={UserName}, Role={Role}, IsAdmin={IsAdmin}",
                        userInfo.UserId, userInfo.UserName, userInfo.Role, userInfo.IsAdmin);
                }
                else
                {
                    _logger.LogWarning("MedicalCase权限验证失败: 用户信息无效");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("用户信息验证失败");
                    return;
                }
            }

            await _next(context);
        }

        #region 私有方法

        /// <summary>
        /// 检查是否为MedicalCase相关请求
        /// </summary>
        private static bool IsMedicalCaseRequest(HttpRequest request)
        {
            var path = request.Path.Value?.ToLowerInvariant();
            if (string.IsNullOrEmpty(path))
                return false;

            return MedicalCasePaths.Any(medicalPath => path.StartsWith(medicalPath, StringComparison.OrdinalIgnoreCase))
                && RestrictedMethods.Contains(request.Method.ToUpperInvariant());
        }

        /// <summary>
        /// 提取用户权限信息
        /// </summary>
        private static MedicalCaseUserInfo? ExtractUserInfo(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return null;

            var claims = context.User.Claims;

            // 获取用户ID
            var userIdClaim = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub ||
                c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;

            // 获取用户名
            var userName = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Name ||
                c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName ||
                c.Type == "unique_name" ||
                c.Type == "name")?.Value ?? context.User.Identity?.Name;

            // Issue #2241: 获取角色并转换为UserRole枚举
            var roleStr = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Role ||
                c.Type == "role" ||
                c.Type == "roles")?.Value;

            var role = ParseUserRole(roleStr);

            // Issue #2241: 检查是否为管理员，使用枚举比较
            var isAdmin = role == UserRole.SuperAdmin || role == UserRole.Admin;

            return new MedicalCaseUserInfo
            {
                UserId = userId,
                UserName = userName ?? "Unknown",
                Role = role,
                IsAdmin = isAdmin,
                // 当天可改规则：管理员或本人当天创建的病案可修改
                CanEditToday = isAdmin
            };
        }

        /// <summary>
        /// 解析用户角色字符串为UserRole枚举
        /// Issue #2241: 与BaseControllerCore.ParseUserRole保持一致
        /// </summary>
        private static UserRole ParseUserRole(string? roleStr)
        {
            if (string.IsNullOrWhiteSpace(roleStr))
            {
                return UserRole.Doctor;
            }

            // 处理遗留命名：SysAdmin → SuperAdmin
            if (roleStr.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
            {
                roleStr = "SuperAdmin";
            }

            // 尝试解析为枚举
            if (Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
            {
                // 检查是否为已废弃的角色
                if (role == UserRole.User ||
                    role == UserRole.Pharmacist ||
                    role == UserRole.Receptionist ||
                    role == UserRole.Cashier ||
                    role == UserRole.Therapist)
                {
                    return UserRole.Doctor;
                }

                return role;
            }

            // 解析失败，使用默认值
            return UserRole.Doctor;
        }

        #endregion
    }

    /// <summary>
    /// MedicalCase用户权限信息
    /// Issue #2241: Role改为UserRole枚举类型
    /// </summary>
    public class MedicalCaseUserInfo
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Doctor;
        public bool IsAdmin { get; set; }
        public bool CanEditToday { get; set; }
    }

    /// <summary>
    /// MedicalCase权限验证中间件扩展
    /// </summary>
    public static class MedicalCasePermissionMiddlewareExtensions
    {
        /// <summary>
        /// 使用MedicalCase权限验证中间件
        /// </summary>
        public static IApplicationBuilder UseMedicalCasePermission(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MedicalCasePermissionMiddleware>();
        }
    }
}