# Issue #806: 优化中间件管道顺序和配置

## 📋 问题描述
当前中间件配置存在问题：
- 中间件顺序不当影响性能
- 缺少响应压缩
- 异常处理位置不合理
- 缺少请求/响应日志
- CORS配置过于宽松

## 🎯 优化目标
- 优化中间件执行顺序
- 减少请求处理延迟20%
- 启用响应压缩减少传输大小
- 完善异常处理和日志记录

## 📁 涉及文件和具体修改

### 1. Program.cs - 重构中间件管道
**文件路径**: `src/Server/Services/LYBT.WebAPI/Program.cs`

#### 优化中间件顺序
```csharp
// 修改前：中间件顺序混乱
app.UseAuthentication();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseCors();

// 修改后：优化的中间件顺序
var app = builder.Build();

// 1. 异常处理（最外层）
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// 2. 请求日志（尽早记录）
app.UseMiddleware<RequestLoggingMiddleware>();

// 3. 安全头
app.UseSecurityHeaders();

// 4. HTTPS重定向
app.UseHttpsRedirection();

// 5. 响应压缩（在静态文件之前）
app.UseResponseCompression();

// 6. 静态文件（尽早返回）
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // 静态文件缓存1年
        ctx.Context.Response.Headers.Append(
            "Cache-Control", "public,max-age=31536000");
    }
});

// 7. 路由
app.UseRouting();

// 8. CORS（在认证之前）
app.UseCors("ProductionPolicy");

// 9. 速率限制
app.UseRateLimiter();

// 10. 响应缓存
app.UseResponseCaching();

// 11. 输出缓存
app.UseOutputCache();

// 12. 认证
app.UseAuthentication();

// 13. 授权
app.UseAuthorization();

// 14. 端点
app.MapControllers();

// 15. 健康检查
app.MapHealthChecks("/health");
```

### 2. 响应压缩配置
**文件路径**: `src/Server/Services/LYBT.WebAPI/Extensions/CompressionExtensions.cs`

```csharp
public static class CompressionExtensions
{
    public static IServiceCollection AddOptimizedCompression(
        this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

            // 压缩的MIME类型
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/xml",
                "text/json",
                "text/xml"
            });
        });

        // Brotli配置（优先）
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        // Gzip配置（备选）
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return services;
    }
}

// 在Program.cs使用
builder.Services.AddOptimizedCompression();
```

### 3. 请求日志中间件
**文件路径**: `src/Server/Core/LYBT.Infrastructure/Middleware/RequestLoggingMiddleware.cs`

```csharp
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 跳过健康检查和静态文件
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/static"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        // 添加请求ID到响应头
        context.Response.Headers.Add("X-Request-Id", requestId);

        try
        {
            // 记录请求
            _logger.LogInformation(
                "[{RequestId}] {Method} {Path} 开始处理",
                requestId,
                context.Request.Method,
                context.Request.Path);

            await _next(context);

            // 记录响应
            _logger.LogInformation(
                "[{RequestId}] {Method} {Path} 完成 - {StatusCode} ({ElapsedMs}ms)",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            // 添加性能头
            context.Response.Headers.Add(
                "X-Response-Time",
                $"{stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] {Method} {Path} 异常 ({ElapsedMs}ms)",
                requestId,
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 4. 全局异常处理
**文件路径**: `src/Server/Core/LYBT.Infrastructure/Middleware/GlobalExceptionMiddleware.cs`

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "未处理的异常");

        context.Response.StatusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Message = _environment.IsDevelopment()
                ? exception.Message
                : "服务器内部错误",
            Details = _environment.IsDevelopment()
                ? exception.StackTrace
                : null,
            RequestId = context.Response.Headers["X-Request-Id"].ToString(),
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
```

### 5. 安全头中间件
**文件路径**: `src/Server/Core/LYBT.Infrastructure/Middleware/SecurityHeadersMiddleware.cs`

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 添加安全头
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        // CSP头
        context.Response.Headers.Add(
            "Content-Security-Policy",
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");

        // 移除不必要的头
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}

// 扩展方法
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
```

### 6. CORS优化配置
**文件路径**: `src/Server/Services/LYBT.WebAPI/Extensions/CorsExtensions.cs`

```csharp
public static class CorsExtensions
{
    public static IServiceCollection AddOptimizedCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            // 开发环境策略
            options.AddPolicy("DevelopmentPolicy", builder =>
            {
                builder.WithOrigins("http://localhost:3000", "http://localhost:5173")
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials()
                       .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });

            // 生产环境策略
            options.AddPolicy("ProductionPolicy", builder =>
            {
                var allowedOrigins = configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? Array.Empty<string>();

                builder.WithOrigins(allowedOrigins)
                       .WithMethods("GET", "POST", "PUT", "DELETE")
                       .WithHeaders("Content-Type", "Authorization")
                       .AllowCredentials()
                       .SetPreflightMaxAge(TimeSpan.FromHours(1));
            });
        });

        return services;
    }
}
```

### 7. 速率限制配置
**文件路径**: `src/Server/Services/LYBT.WebAPI/Extensions/RateLimitingExtensions.cs`

```csharp
public static class RateLimitingExtensions
{
    public static IServiceCollection AddOptimizedRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 全局限制
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // API特定限制
            options.AddPolicy("ApiLimiter", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? "anonymous",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // 登录限制
            options.AddPolicy("LoginLimiter", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5)
                    }));
        });

        return services;
    }
}
```

### 8. 健康检查配置
**文件路径**: `src/Server/Services/LYBT.WebAPI/Extensions/HealthCheckExtensions.cs`

```csharp
public static class HealthCheckExtensions
{
    public static IServiceCollection AddOptimizedHealthChecks(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddHealthChecks()
            // 数据库健康检查
            .AddSqlServer(
                connectionString,
                name: "database",
                timeout: TimeSpan.FromSeconds(3),
                tags: new[] { "db", "sql" })
            // 内存健康检查
            .AddPrivateMemoryHealthCheck(
                maximumMemoryBytes: 500_000_000, // 500MB
                name: "memory",
                tags: new[] { "memory" })
            // 自定义检查
            .AddCheck<CustomHealthCheck>("custom");

        // 健康检查UI
        services.AddHealthChecksUI(options =>
        {
            options.SetEvaluationTimeInSeconds(60);
            options.MaximumHistoryEntriesPerEndpoint(50);
        })
        .AddInMemoryStorage();

        return services;
    }
}

public class CustomHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查关键服务
            using var scope = _serviceProvider.CreateScope();
            var herbService = scope.ServiceProvider.GetRequiredService<IHerbService>();

            // 简单的可用性检查
            var herbs = await herbService.GetAllAsync(cancellationToken);

            return HealthCheckResult.Healthy("服务正常运行");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("服务异常", ex);
        }
    }
}
```

## ✅ 验收标准
1. 中间件按照正确顺序配置
2. 响应压缩正常工作
3. 请求日志记录完整
4. 全局异常处理正确
5. 安全头配置到位
6. CORS和速率限制生效

## 🔧 实施步骤
1. [ ] 重构Program.cs中间件顺序
2. [ ] 实现响应压缩
3. [ ] 添加请求日志中间件
4. [ ] 实现全局异常处理
5. [ ] 配置安全头
6. [ ] 优化CORS和速率限制
7. [ ] 添加健康检查端点

## 📊 预期效果
- 请求处理延迟：降低20%
- 响应大小：压缩后减少60%
- 异常处理：100%覆盖
- 安全评分：提升至A级

## 🏷️ 标签
`performance` `middleware` `security` `optimization` `mvp`

## 📎 相关文档
- [ASP.NET Core Middleware](https://docs.microsoft.com/aspnet/core/fundamentals/middleware)
- [Response Compression](https://docs.microsoft.com/aspnet/core/performance/response-compression)

---
**优先级**: P1（高）
**预估工时**: 1天
**负责人**: 待分配
**状态**: 待开始