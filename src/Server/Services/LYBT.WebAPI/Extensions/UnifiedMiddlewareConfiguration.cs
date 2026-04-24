using LYBT.WebAPI.Configuration;
using LYBT.WebAPI.Middleware;

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
    /// <summary>
    /// 配置应用中间件（统一入口）
    /// 优化后的中间件管道顺序，遵循ASP.NET Core最佳实践
    /// </summary>
    public static WebApplication ConfigureAllMiddleware(this WebApplication app)
    {
        // ===== 阶段1: 错误处理和安全 =====
        // 1.1 统一异常处理(所有环境使用相同JSON格式)
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

                if (exception == null)
                {
                    return;
                }

                // Resolve and invoke IExceptionHandler chain (BusinessExceptionHandler → SystemExceptionHandler)
                // CRITICAL: Use Microsoft.AspNetCore.Diagnostics.IExceptionHandler (NOT LYBT.Shared.ExceptionHandling.Handlers.IExceptionHandler)
                var handlers = context.RequestServices.GetServices<Microsoft.AspNetCore.Diagnostics.IExceptionHandler>();
                
                foreach (var handler in handlers)
                {
                    var handled = await handler.TryHandleAsync(context, exception, context.RequestAborted);
                    if (handled)
                    {
                        return; // Handler wrote ApiResponse to response stream
                    }
                }

                // Fallback: No handler processed exception, write generic ApiResponse
                context.Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                
                var fallbackResponse = LYBT.Shared.Models.Contracts.Common.ApiResponse.CreateFail(
                    app.Environment.IsDevelopment() 
                        ? $"[DEV] {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}" 
                        : "An unexpected error occurred"
                );
                fallbackResponse.RequestId = context.TraceIdentifier;
                
                await context.Response.WriteAsJsonAsync(fallbackResponse);
            });
        });

        // 1.1.1 StatusCodePages（处理非异常的HTTP错误状态码）
        // refactor-logging-system: RFC 7807标准化状态码响应
        app.UseStatusCodePages(async context =>
        {
            var statusCode = context.HttpContext.Response.StatusCode;
            if (statusCode < 400) return;

            var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? context.HttpContext.TraceIdentifier;
            var traceId = context.HttpContext.TraceIdentifier;

            var errorResponse = LYBT.Shared.Models.Contracts.Common.ApiResponse.CreateFail(
                $"请求处理失败 (HTTP {statusCode})",
                new
                {
                    statusCode,
                    path = context.HttpContext.Request.Path.Value,
                    correlationId,
                    traceId,
                    timestamp = DateTime.UtcNow
                }
            );
            errorResponse.RequestId = traceId;

            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(errorResponse);
        });

        // 1.2 CorrelationId追踪（尽早注册，确保所有后续日志都包含追踪ID）
        // refactor-logging-system: 实现端到端请求追踪
        app.UseCorrelationId();

        // 1.3 HTTPS重定向和HSTS（生产环境）
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        // 1.4 安全响应头
        app.UseSecurityHeaders();

        // ===== 阶段2: 性能优化（早期执行） =====
        // 2.1 响应压缩（必须在写入响应之前）
        app.UseResponseCompression();

        // 2.2 Desktop 发布包静态文件（条件启用）
        var releasesPath = app.Configuration["DesktopUpdate:ReleasesPath"];
        if (app.Configuration.GetValue<bool>("DesktopUpdate:Enabled") && !string.IsNullOrEmpty(releasesPath) && Directory.Exists(releasesPath))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(releasesPath),
                RequestPath = app.Configuration["DesktopUpdate:DownloadBaseUrl"] ?? "/releases",
                ServeUnknownFileTypes = false,
                DefaultContentType = "application/octet-stream"
            });
            app.Logger.LogInformation("Desktop 发布包静态文件已启用: {Path} -> {UrlPath}", releasesPath, app.Configuration["DesktopUpdate:DownloadBaseUrl"] ?? "/releases");
        }

        // ===== 阶段3: 路由和请求处理 =====
        // 3.0 Swagger（在路由和认证之前，避免被 FallbackPolicy 拦截）
        app.ConfigureSwaggerMiddleware();

        // 3.1 路由（必须在认证之前）
        app.UseRouting();

        // 3.2 速率限制 - A2-02: 启用速率限制中间件
        app.UseRateLimiter();

        // ===== 阶段4: 认证和授权 =====
        // 4.1 认证
        app.UseAuthentication();

        // 4.2 Claims标准化（在认证后，授权前）
        app.UseClaimsNormalization();

        // 4.3 授权
        // refactor-authorization-system: MedicalCase权限现通过 IAuthorizationService 资源级授权实现
        // 已删除 UseMedicalCasePermission() 中间件
        app.UseAuthorization();

        // ===== 阶段5: 缓存（在认证授权后） =====
        // 5.1 响应缓存
        app.UseResponseCaching();

        // 5.2 输出缓存（.NET 7+）
        app.UseOutputCache();

        // ===== 阶段6: 终端映射（最后） =====
        // Issue #1726 Phase 3: 健康检查端点
        // Sprint3-A3-08: FallbackPolicy 启用后，健康检查需显式 AllowAnonymous
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapHealthChecks("/health/database", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = (check) => check.Name == "database"
        }).AllowAnonymous();

        app.MapControllers();

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

}
