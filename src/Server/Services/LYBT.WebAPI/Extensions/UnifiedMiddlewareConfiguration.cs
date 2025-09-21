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

        // 2. 路由中间件 - 提升到顶层统一调用
        app.UseRouting();

        // 3. API文档中间件
        app.ConfigureSwaggerMiddleware();

        // 4. 认证和授权中间件（不再包含UseRouting）
        app.ConfigureAuthenticationMiddleware();

        // 5. 端点映射
        app.ConfigureEndpointMapping();

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

        // Epic 05-P0-02 修复：启用全局异常处理器
        app.UseExceptionHandler();

        return app;
    }

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
        // UseRouting已在ConfigureAllMiddleware顶层调用

        // CORS已移除：WPF+WebAPI架构无需跨域支持

        // 认证和授权
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// 配置端点映射
    /// </summary>
    private static WebApplication ConfigureEndpointMapping(this WebApplication app)
    {
        // 控制器端点映射
        app.MapControllers();

        return app;
    }
}
