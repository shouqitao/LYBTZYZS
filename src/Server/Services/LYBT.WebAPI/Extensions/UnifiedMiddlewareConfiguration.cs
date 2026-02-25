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
        // 1.1 异常处理（最外层，捕获所有异常）
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler();
        }

        // 1.1.1 StatusCodePages（处理非异常的HTTP错误状态码）
        // refactor-logging-system: RFC 7807标准化状态码响应
        app.UseStatusCodePagesWithProblemDetails();

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

        // 2.2 静态文件（如果有）
        // app.UseStaticFiles(); // 当前项目无静态文件

        // ===== 阶段3: 路由和请求处理 =====
        // 3.1 路由（必须在认证之前）
        app.UseRouting();

        // 3.2 CORS（如需要，在认证之前）
        // app.UseCors(); // 根据需要启用

        // 3.3 速率限制 - A2-02: 启用速率限制中间件
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

        // ===== 阶段6: API文档（可选） =====
        // 6.1 Swagger（仅非生产）
        app.ConfigureSwaggerMiddleware();

        // ===== 阶段7: 终端映射（最后） =====
        // Issue #1726 Phase 3: 健康检查端点
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/database", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = (check) => check.Name == "database"
        });

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
