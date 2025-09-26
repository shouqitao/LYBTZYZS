using System.IdentityModel.Tokens.Jwt;
using LYBT.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

namespace LYBT.WebAPI.Middleware
{
    /// <summary>
    /// JWT黑名单验证中间件
    /// 在JWT认证后检查Token是否在黑名单中，防止被撤销的Token继续使用
    /// </summary>
    public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtBlacklistMiddleware> _logger;

        public JwtBlacklistMiddleware(RequestDelegate next, ILogger<JwtBlacklistMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
        {
            try
            {
                // 只对已认证的请求进行黑名单检查
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    // 检查端点是否需要授权
                    var endpoint = context.GetEndpoint();
                    var hasAuthorizeAttribute = endpoint?.Metadata?.GetMetadata<AuthorizeAttribute>() != null;
                    var hasAllowAnonymousAttribute = endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null;

                    // 只对需要授权的端点进行检查
                    if (hasAuthorizeAttribute && !hasAllowAnonymousAttribute)
                    {
                        var isBlacklisted = await blacklistService.IsTokenBlacklistedAsync(context.User);

                        if (isBlacklisted)
                        {
                            var jwtId = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? "未知";
                            var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "未知";

                            _logger.LogWarning(
                                "阻止黑名单Token访问，JwtId: {JwtId}, UserId: {UserId}, Path: {Path}, IP: {IP}",
                                jwtId, userId, context.Request.Path, context.Connection.RemoteIpAddress);

                            // 返回401未授权
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";

                            var errorResponse = new
                            {
                                error = "token_revoked",
                                message = "Token已被撤销，请重新登录",
                                timestamp = DateTime.UtcNow,
                                path = context.Request.Path.ToString()
                            };

                            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
                            return;
                        }
                    }
                }

                // Token验证通过或不需要验证，继续处理请求
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT黑名单验证中间件发生错误");
                
                // 发生错误时继续处理请求，避免影响正常功能
                await _next(context);
            }
        }
    }

    /// <summary>
    /// JWT黑名单中间件扩展方法
    /// </summary>
    public static class JwtBlacklistMiddlewareExtensions
    {
        /// <summary>
        /// 使用JWT黑名单验证中间件
        /// </summary>
        /// <param name="builder">应用程序构建器</param>
        /// <returns>应用程序构建器</returns>
        public static IApplicationBuilder UseJwtBlacklist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtBlacklistMiddleware>();
        }
    }
}