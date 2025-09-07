namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 统一中间件配置管理 - UltraThink中间件配置系统
/// 将所有中间件配置逻辑统一管理，确保正确的执行顺序
/// </summary>
public static class UnifiedMiddlewareConfiguration
{

    /// <summary>
    /// 配置所有应用中间件（统一入口）
    /// </summary>
    public static WebApplication ConfigureAllMiddleware(this WebApplication app)
    {
        // 1. 开发环境专用中间件
        app.ConfigureDevelopmentMiddleware();

        // 2. 安全和性能中间件 (简化版 - 仅基础功能)
        // app.ConfigureSecurityMiddleware(); // Removed - enterprise security features

        // 3. API文档中间件
        app.ConfigureSwaggerMiddleware();

        // 4. 认证和授权中间件
        app.ConfigureAuthenticationMiddleware();

        // 5. 路由中间件
        app.ConfigureRoutingMiddleware();

        return app;
    }

    /// <summary>
    /// 配置开发环境专用中间件
    /// </summary>
    private static WebApplication ConfigureDevelopmentMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // 开发环境异常页面
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // 生产环境HTTPS重定向和安全头
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        return app;
    }

    // /// <summary>
    // /// 配置安全和性能中间件 - REMOVED (enterprise features beyond constraint)
    // /// </summary>
    // private static WebApplication ConfigureSecurityMiddleware(this WebApplication app)
    // {
    //     // Features removed to comply with UltraThink constraints
    //     return app;
    // }

    /// <summary>
    /// 配置Swagger API文档中间件
    /// </summary>
    private static WebApplication ConfigureSwaggerMiddleware(this WebApplication app)
    {
        // 启用Swagger（优先级最高）
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "LYBT API v1");
            c.RoutePrefix = "swagger";
            c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        });

        return app;
    }

    /// <summary>
    /// 配置认证和授权中间件
    /// </summary>
    private static WebApplication ConfigureAuthenticationMiddleware(this WebApplication app)
    {
        // CORS配置
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("Development");
        }
        else
        {
            app.UseCors("Production");
        }

        // 认证和授权
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// 配置路由中间件
    /// </summary>
    private static WebApplication ConfigureRoutingMiddleware(this WebApplication app)
    {
        // 控制器路由
        app.MapControllers();

        return app;
    }
}
