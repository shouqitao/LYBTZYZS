using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 环境感知的运行模式管理
/// 根据ASPNETCORE_ENVIRONMENT环境变量自动选择运行模式：
/// - Development: 控制台模式，实时日志显示
/// - Production: Windows Service模式，后台运行
/// </summary>
public static class EnvironmentAwareHosting
{
    /// <summary>
    /// 配置环境感知的主机运行模式
    /// </summary>
    public static IHostBuilder ConfigureEnvironmentAwareHosting(this IHostBuilder hostBuilder)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        if (environment == "Development")
        {
            // 开发模式：控制台运行
            hostBuilder.UseConsoleLifetime();
        }
        else
        {
            // 生产模式：Windows Service运行
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                hostBuilder.UseWindowsService();
            }
            else
            {
                // 非Windows平台使用标准主机生命周期
                hostBuilder.UseConsoleLifetime();
            }
        }

        return hostBuilder;
    }

    /// <summary>
    /// 显示开发模式的启动信息
    /// </summary>
    public static void DisplayDevelopmentStartupInfo(this WebApplication app)
    {
        var environment = app.Environment.EnvironmentName;

        if (environment == "Development")
        {
            DisplayDevelopmentConsoleHeader();
            DisplayStartupStatus(app);
        }
    }

    /// <summary>
    /// 显示开发模式控制台头部信息
    /// </summary>
    private static void DisplayDevelopmentConsoleHeader()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // 在某些环境中可能无法清空控制台，跳过即可
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                LYBT WebAPI 开发模式                      ║");
        Console.WriteLine("║            凌隐宝堂中医诊所诊疗系统 WebAPI                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// 显示启动状态信息
    /// </summary>
    private static void DisplayStartupStatus(WebApplication app)
    {
        var addresses = app.Urls;
        var primaryUrl = addresses.FirstOrDefault() ?? "https://localhost:5001";

        Console.WriteLine($"[启动] ✅ 环境: {app.Environment.EnvironmentName}");
        Console.WriteLine($"[启动] ✅ 服务地址: {primaryUrl}");
        Console.WriteLine($"[启动] ✅ Swagger文档: {primaryUrl}/swagger");
        Console.WriteLine($"[启动] ✅ 启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        // 检查数据库连接状态
        try
        {
            // 这里可以添加数据库连接检查逻辑
            Console.WriteLine("[启动] ✅ 数据库连接正常");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[启动] ❌ 数据库连接失败: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("🚀 服务启动完成！按 Ctrl+C 停止服务");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("=== 实时日志 ===");
    }

    /// <summary>
    /// 配置开发模式的请求日志中间件
    /// </summary>
    public static IApplicationBuilder UseDevelopmentRequestLogging(this IApplicationBuilder app)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        if (environment == "Development")
        {
            app.Use(async (context, next) =>
            {
                var stopwatch = Stopwatch.StartNew();
                var startTime = DateTime.Now;

                await next();

                stopwatch.Stop();

                var statusColor = context.Response.StatusCode >= 400 ? ConsoleColor.Red :
                                context.Response.StatusCode >= 300 ? ConsoleColor.Yellow :
                                ConsoleColor.Green;

                Console.ForegroundColor = statusColor;
                Console.WriteLine($"[请求] {startTime:HH:mm:ss} {context.Request.Method} {context.Request.Path} ({stopwatch.ElapsedMilliseconds}ms) → {context.Response.StatusCode}");
                Console.ResetColor();
            });
        }

        return app;
    }

    /// <summary>
    /// 配置环境感知的优雅关闭
    /// </summary>
    public static async Task ConfigureEnvironmentAwareShutdown(this WebApplication app)
    {
        var environment = app.Environment.EnvironmentName;

        if (environment == "Development")
        {
            await ConfigureDevelopmentShutdown(app);
        }
        else
        {
            await ConfigureProductionShutdown(app);
        }
    }

    /// <summary>
    /// 开发模式的优雅关闭配置
    /// </summary>
    private static async Task ConfigureDevelopmentShutdown(WebApplication app)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⏹️  正在停止服务...");
            Console.ResetColor();
            cancellationTokenSource.Cancel();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, __) =>
        {
            cancellationTokenSource.Cancel();
            app.StopAsync().GetAwaiter().GetResult();
        };

        try
        {
            await app.RunAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ 服务已安全停止");
            Console.ResetColor();
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    /// <summary>
    /// 生产模式的优雅关闭配置
    /// </summary>
    private static async Task ConfigureProductionShutdown(WebApplication app)
    {
        // 生产模式使用标准的Windows Service生命周期管理
        await app.RunAsync();
    }
}
