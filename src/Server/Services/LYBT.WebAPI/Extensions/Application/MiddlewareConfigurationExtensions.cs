using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LYBT.WebAPI.Extensions.Application
{
    /// <summary>
    /// 中间件配置扩展方法 - 已废弃，请使用UnifiedMiddlewareConfiguration
    /// </summary>
    [Obsolete("已废弃：请使用UnifiedMiddlewareConfiguration进行中间件配置。此类将在下个版本移除。", false)]
    public static class MiddlewareConfigurationExtensions
    {
        /// <summary>
        /// 配置基础中间件
        /// </summary>
        /// <param name="app">应用程序构建器</param>
        /// <param name="env">Web主机环境</param>
        /// <returns>配置后的应用程序构建器</returns>
        public static IApplicationBuilder ConfigureBasicMiddleware(
            this IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            return app;
        }

        /// <summary>
        /// 配置认证中间件
        /// </summary>
        /// <param name="app">应用程序构建器</param>
        /// <returns>配置后的应用程序构建器</returns>
        public static IApplicationBuilder ConfigureAuthenticationMiddleware(
            this IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }

        /// <summary>
        /// 配置Swagger中间件
        /// </summary>
        /// <param name="app">应用程序构建器</param>
        /// <param name="apiTitle">API标题</param>
        /// <param name="apiVersion">API版本</param>
        /// <param name="routePrefix">路由前缀</param>
        /// <returns>配置后的应用程序构建器</returns>
        public static IApplicationBuilder ConfigureSwagger(
            this IApplicationBuilder app,
            string apiTitle = "API",
            string apiVersion = "v1",
            string routePrefix = "swagger")
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", $"{apiTitle} {apiVersion}");
                options.RoutePrefix = routePrefix;
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            });

            return app;
        }

        /// <summary>
        /// 配置安全头中间件
        /// </summary>
        /// <param name="app">应用程序构建器</param>
        /// <param name="enableHsts">是否启用HSTS</param>
        /// <param name="enableXssProtection">是否启用XSS保护</param>
        /// <param name="enableFrameOptions">是否启用Frame选项</param>
        /// <returns>配置后的应用程序构建器</returns>
        public static IApplicationBuilder ConfigureSecurityHeaders(
            this IApplicationBuilder app,
            bool enableHsts = true,
            bool enableXssProtection = true,
            bool enableFrameOptions = true)
        {
            app.Use(async (context, next) =>
            {
                if (enableXssProtection)
                {
                    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                }

                if (enableFrameOptions)
                {
                    context.Response.Headers["X-Frame-Options"] = "DENY";
                }

                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                await next();
            });

            if (enableHsts)
            {
                app.UseHsts();
            }

            return app;
        }

        /// <summary>
        /// 配置请求日志中间件
        /// </summary>
        /// <param name="app">应用程序构建器</param>
        /// <param name="logRequestBody">是否记录请求体</param>
        /// <param name="logResponseBody">是否记录响应体</param>
        /// <returns>配置后的应用程序构建器</returns>
        public static IApplicationBuilder ConfigureRequestLogging(
            this IApplicationBuilder app,
            bool logRequestBody = false,
            bool logResponseBody = false)
        {
            app.Use(async (context, next) =>
            {
                var startTime = DateTime.UtcNow;

                // 记录请求信息
                var requestInfo = $"{context.Request.Method} {context.Request.Path}";
                if (context.Request.QueryString.HasValue)
                {
                    requestInfo += context.Request.QueryString.Value;
                }

                await next();

                // 记录响应信息
                var elapsed = DateTime.UtcNow - startTime;
                var responseInfo = $"Status: {context.Response.StatusCode}, Time: {elapsed.TotalMilliseconds}ms";

                // 这里可以集成日志框架记录
                Console.WriteLine($"[{startTime:yyyy-MM-dd HH:mm:ss}] {requestInfo} -> {responseInfo}");
            });

            return app;
        }
    }
}