using LYBT.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LYBT.Infrastructure.Middleware
{
    /// <summary>
    /// JWT黑名单检查中间件
    /// 拦截请求并验证JWT是否在黑名单中
    /// </summary>
    public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtBlacklistMiddleware> _logger;

        public JwtBlacklistMiddleware(RequestDelegate next, ILogger<JwtBlacklistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
        {
            try
            {
                // 只检查已认证的请求
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    // 从用户Claims中获取JWT ID
                    var jwtIdClaim = context.User.FindFirst(JwtRegisteredClaimNames.Jti);
                    
                    if (jwtIdClaim != null && !string.IsNullOrWhiteSpace(jwtIdClaim.Value))
                    {
                        var jwtId = jwtIdClaim.Value;
                        
                        // 检查是否在黑名单中
                        var isBlacklisted = await blacklistService.IsTokenBlacklistedAsync(jwtId);
                        
                        if (isBlacklisted)
                        {
                            _logger.LogWarning("检测到黑名单JWT Token: {JwtId}, IP: {ClientIP}", 
                                jwtId, context.Connection.RemoteIpAddress);

                            // 返回401未授权
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            
                            var response = new
                            {
                                success = false,
                                message = "Token已被撤销",
                                errorCode = "TOKEN_REVOKED",
                                timestamp = DateTime.UtcNow
                            };

                            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                            return;
                        }
                    }
                    else
                    {
                        // 如果JWT没有JTI claim，记录警告（但允许继续）
                        _logger.LogWarning("JWT Token缺少JTI claim, IP: {ClientIP}", 
                            context.Connection.RemoteIpAddress);
                    }
                }

                // 继续处理请求
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT黑名单检查中间件发生异常");
                
                // 异常时为了安全起见，拒绝请求
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        success = false,
                        message = "身份验证检查失败",
                        errorCode = "AUTH_CHECK_FAILED",
                        timestamp = DateTime.UtcNow
                    };

                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
                    return;
                }
                
                // 对于未认证的请求，继续处理
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
        /// 添加JWT黑名单检查中间件
        /// </summary>
        /// <param name="builder">应用程序构建器</param>
        /// <returns>应用程序构建器</returns>
        public static IApplicationBuilder UseJwtBlacklistCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtBlacklistMiddleware>();
        }
    }
}