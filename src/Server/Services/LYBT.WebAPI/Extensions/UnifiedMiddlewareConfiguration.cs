using LYBT.Shared.Logging.Http;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

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
        // 1.1 统一异常处理(所有环境使用相同 ProblemDetails 格式)
        // X-4 管道化：Business/SystemExceptionHandler 已经 AddExceptionHandler 注册（ExceptionHandlingServiceCollectionExtensions），
        // ExceptionHandlerMiddleware 会先遍历 IExceptionHandler 链；本委托仅作未处理异常的 ProblemDetails 兜底，
        // 不再手动 resolve/foreach Handler（避免双重处理）。
        // X-3: 异常路径统一 ProblemDetails（RFC 7807）；ApiResponse 仅用于成功/已知业务失败响应。
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

                if (exception == null)
                {
                    return;
                }

                // Fallback: No IExceptionHandler processed exception, write generic ProblemDetails
                await Results.Problem(
                    statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError,
                    title: "服务器内部错误",
                    detail: app.Environment.IsDevelopment()
                        ? $"[DEV] {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
                        : "An unexpected error occurred",
                    instance: context.Request.Path)
                    .ExecuteAsync(context);
            });
        });

        // 1.1.1 StatusCodePages（处理非异常的HTTP错误状态码）
        // refactor-logging-system: RFC 7807标准化状态码响应
        // X-3: 统一 ProblemDetails（与异常路径同契约）
        app.UseStatusCodePages(async context =>
        {
            var statusCode = context.HttpContext.Response.StatusCode;
            if (statusCode < 400) return;

            await Results.Problem(
                statusCode: statusCode,
                title: $"HTTP {statusCode}",
                detail: $"请求处理失败 (HTTP {statusCode})",
                instance: context.HttpContext.Request.Path)
                .ExecuteAsync(context.HttpContext);
        });

        // 1.2 转发头处理（必须在 CorrelationId 之前，用于反向代理场景）
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // 1.3 CorrelationId追踪（尽早注册，确保所有后续日志都包含追踪ID）
        // refactor-logging-system: 实现端到端请求追踪（A-31-C1: UseLybtCorrelationId 单点注册）
        app.UseLybtCorrelationId();

        // 1.3 HSTS 与 HTTPS 重定向（生产环境）
        // P2-2: UseHsts 必须先于 UseHttpsRedirection（官方文档顺序——HSTS 响应头需写在重定向前）
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
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

        // 3.1.0 CORS（在UseRouting之后、认证/授权之前）
        app.UseCors("AllowConfiguredOrigins");

        // 3.1.1 Serilog请求日志
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            };
        });

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

        // ===== 阶段5: 终端映射（最后） =====
        // Issue #1726 Phase 3: 健康检查端点 - P2-12-1 根路由探针分离：/health 为外部探针（匿名，K8s/负载均衡），
        // /api/v1/health 为业务受控探针（需认证的 GetDetails），根 "/" 保持 404 不混为探针
        // Sprint3-A3-08: FallbackPolicy 启用后，健康检查需显式 AllowAnonymous
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapHealthChecks("/health/database", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = (check) => check.Name == "database"
        }).AllowAnonymous();

        app.MapControllers();

        // SWAGGER-ANON: Swagger 启用时注册匿名兜底端点——FallbackPolicy（RequireAuthenticatedUser）
        // 对无端点的 /swagger 请求返回 401；此端点使 swagger 路径有 AllowAnonymous 端点（授权豁免）。
        // SwaggerUI 中间件正常时短路 200；异常时 404（不暴露存在性）。
        // 注：必须在 UseRouting 之后注册（MapGet 是终端路由）；仅 Swagger 启用时注册。
        var swaggerEnabledForAnon = LYBT.WebAPI.Configuration.SwaggerAvailability.IsEnabled(app.Configuration, app.Environment);
        if (swaggerEnabledForAnon)
        {
            app.MapGet("/swagger/{**path}", () => Results.NotFound())
                .WithMetadata(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute())
                // B-16: 匿名兜底端点非业务 API——排除出 OpenAPI 文档（否则 swagger.json 出现 /swagger/{path} 伪端点）
                .ExcludeFromDescription();
        }

        return app;
    }

    /// <summary>
    /// 配置 Swagger API 文档
    /// </summary>
    private static WebApplication ConfigureSwaggerMiddleware(this WebApplication app)
    {
        // SWAGGER-TOGGLE: 非生产默认启用；生产默认关闭（Swagger:Enabled=true 可在线启用——测试发布调试用）
        if (LYBT.WebAPI.Configuration.SwaggerAvailability.IsEnabled(app.Configuration, app.Environment))
        {
            app.UseSwagger();

            // F-07: SwaggerUI 端点按发现到的 API 版本逐个注册（与 ConfigureSwaggerOptions 同一 provider）——
            // 只有 v1 时展示名/路径与历史一致（"凌隐宝堂中医诊所 API v1" → /swagger/v1/swagger.json）
            var versionProvider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
            var swaggerConfig = new LYBT.Shared.Configuration.Options.Server.SwaggerOptions();
            app.Configuration.GetSection(LYBT.Shared.Configuration.Options.Server.SwaggerOptions.SectionName).Bind(swaggerConfig);

            app.UseSwaggerUI(c =>
            {
                foreach (var description in versionProvider.ApiVersionDescriptions)
                {
                    c.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"{swaggerConfig.Title} {description.GroupName}{(description.IsDeprecated ? " (已弃用)" : string.Empty)}");
                }

                c.RoutePrefix = "swagger";
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            });
        }

        return app;
    }

}


