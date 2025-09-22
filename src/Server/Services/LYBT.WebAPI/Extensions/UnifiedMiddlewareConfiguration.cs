using LYBT.Infrastructure.Configuration.Options;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 统一中间件配置（UltraThink 中间件装配体系）
/// 将应用中间件装配逻辑统一在此，保证顺序正确、行为一致。
/// </summary>
public static class UnifiedMiddlewareConfiguration
{
    /// <summary>
    /// 配置应用中间件（统一入口）
    /// </summary>
    public static WebApplication ConfigureAllMiddleware(this WebApplication app)
    {
        // 1. 开发/生产专用中间件
        app.ConfigureDevelopmentMiddleware();

        // 1.1 安全响应头（使用新的中间件）
        app.UseSecurityHeaders();

        // 2. 路由中间件
        app.UseRouting();

        // 2.1 速率限制（全局）
        app.UseRateLimiter();

        // 2.2 性能优化（压缩/响应缓存/输出缓存）
        app.UsePerformanceOptimizations();

        // 3. API 文档中间件（仅非生产）
        app.ConfigureSwaggerMiddleware();

        // 4. 认证与授权（置于路由之后）
        app.ConfigureAuthenticationMiddleware();

        // 5. 终端映射
        app.ConfigureEndpointMapping();

        return app;
    }

    /// <summary>
    /// 配置开发/生产专用中间件
    /// </summary>
    private static WebApplication ConfigureDevelopmentMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // 开发异常页
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // 生产启用 HTTPS 重定向与 HSTS
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        // 统一全局异常处理（ProblemDetails）
        app.UseExceptionHandler();
        return app;
    }

    /// <summary>
    /// 配置 Swagger API 文档
    /// </summary>
    private static WebApplication ConfigureSwaggerMiddleware(this WebApplication app)
    {
        // 仅在非生产环境启用 Swagger
        if (!app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "凌隐宝堂中医诊所 API v1");
                c.RoutePrefix = "swagger";
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            });
        }

        return app;
    }

    /// <summary>
    /// 配置认证与授权中间件
    /// </summary>
    private static WebApplication ConfigureAuthenticationMiddleware(this WebApplication app)
    {
        // 认证
        app.UseAuthentication();

        // Claims标准化（在认证后，授权前）
        app.UseClaimsNormalization();

        // 授权
        app.UseAuthorization();
        return app;
    }

    /// <summary>
    /// 配置终端映射
    /// </summary>
    private static WebApplication ConfigureEndpointMapping(this WebApplication app)
    {
        // 常规控制器路由
        app.MapControllers();
        return app;
    }
}

/// <summary>
/// 安全响应头：按配置应用 CSP/Frame/Referrer/CTO/Permissions-Policy 等
/// </summary>
internal static class SecurityHeadersMiddleware
{
    public static WebApplication ConfigureSecurityHeadersFromOptions(this WebApplication app)
    {
        var options = app.Services.GetService<IOptions<SecurityOptions>>()?.Value;
        if (options == null)
        {
            return app;
        }

        var headers = options.SecurityHeaders;
        app.Use(async (context, next) =>
        {
            if (!string.IsNullOrWhiteSpace(headers.ContentSecurityPolicy))
            {
                context.Response.Headers["Content-Security-Policy"] = headers.ContentSecurityPolicy;
            }
            if (!string.IsNullOrWhiteSpace(headers.XFrameOptions))
            {
                context.Response.Headers["X-Frame-Options"] = headers.XFrameOptions;
            }
            if (!string.IsNullOrWhiteSpace(headers.XContentTypeOptions))
            {
                context.Response.Headers["X-Content-Type-Options"] = headers.XContentTypeOptions;
            }
            if (!string.IsNullOrWhiteSpace(headers.ReferrerPolicy))
            {
                context.Response.Headers["Referrer-Policy"] = headers.ReferrerPolicy;
            }
            if (!string.IsNullOrWhiteSpace(headers.PermissionsPolicy))
            {
                context.Response.Headers["Permissions-Policy"] = headers.PermissionsPolicy;
            }
            await next();
        });

        return app;
    }
}

