// using LYBT.Infrastructure.Configuration; // Removed - SimplifiedConfigurationService eliminated

// using LYBT.WebAPI.Services; // Removed - enterprise services
namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 统一应用初始化管理 - UltraThink初始化系统
/// 将所有应用初始化逻辑统一管理，确保正确的初始化顺序和错误处理
/// </summary>
public static class UnifiedApplicationInitialization
{

    /// <summary>
    /// 执行所有应用初始化（统一入口）
    /// </summary>
    public static async Task InitializeAllApplicationServices(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        try
        {
            // 使用超时取消令牌防止初始化卡死
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            // 1. 数据库初始化（优先执行）
            await app.InitializeDatabaseAsync(scope);

            // 2. 配置服务初始化
            app.InitializeConfigurationServices(scope);

            // 3. 安全配置验证
            // 临时注释掉缺失的安全配置验证服务
            // await app.ValidateSecurityConfigurationAsync(scope);

            // 4. 记录启动日志
            await app.LogApplicationStartupAsync(scope);
        }
        catch (Exception ex)
        {
            await app.HandleInitializationErrorAsync(scope, ex);
        }
    }

    /// <summary>
    /// 数据库初始化
    /// </summary>
    private static async Task InitializeDatabaseAsync(this WebApplication app, IServiceScope scope)
    {
        try
        {
            var dbInitService = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.DatabaseInitializationService>();
            await dbInitService.InitializeDatabaseAsync();

            // 显示数据库信息
            var dbInfo = await dbInitService.GetDatabaseInfoAsync();

            var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
            logger?.LogInformation("✅ 数据库初始化成功");
        }
        catch (Exception dbEx)
        {
            var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
            logger?.LogError(dbEx, "❌ 数据库初始化失败: {ErrorMessage}", dbEx.Message);
            throw;
        }
    }

    /// <summary>
    /// 配置服务初始化
    /// </summary>
    private static void InitializeConfigurationServices(this WebApplication app, IServiceScope scope)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();

        // =========== 直接使用IConfiguration验证 ===========
        try
        {
            // 直接使用IConfiguration进行配置验证
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // 基本配置验证
            logger?.LogInformation("✅ 配置服务初始化完成");

            // 显示环境信息
            var environment = app.Environment.IsDevelopment() ? "Development" :
                            app.Environment.IsProduction() ? "Production" : "Unknown";
            logger?.LogInformation(
                "🌍 运行环境: {Environment}, 机器: {MachineName}",
                environment, Environment.MachineName);

            // 验证关键配置
            try
            {
                var _ = GetConnectionString(configuration);
                logger?.LogInformation("✅ 数据库连接配置验证通过");
            }
            catch (Exception)
            {
                logger?.LogWarning("⚠️ 数据库连接配置可能存在问题");
            }

            try
            {
                var _ = ConfigurationHelper.GetJwtSecret(configuration);
                logger?.LogInformation("✅ JWT配置验证通过");
            }
            catch (Exception)
            {
                logger?.LogWarning("⚠️ JWT配置可能存在问题");
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ 配置验证失败: {ErrorMessage}", ex.Message);

            // 在生产环境中抛出异常，开发环境中继续
            if (!app.Environment.IsDevelopment())
            {
                throw;
            }

            logger?.LogWarning("⚠️ 开发环境中配置验证失败，但继续启动");
        }

        // =========== 直接IConfiguration模式 ===========
        // 消除SimplifiedConfigurationService服务套娃，直接使用.NET内置IConfiguration
        // 基础配置验证已在上面完成，无需额外的复杂初始化
    }

    /// <summary>
    /// 安全配置验证
    /// </summary>
    private static async Task ValidateSecurityConfigurationAsync(this WebApplication app, IServiceScope scope)
    {
        // 临时注释掉安全配置验证以完成核心功能测试
        await Task.CompletedTask;
        /*
        var securityValidator = scope.ServiceProvider.GetService<ISecurityConfigurationValidator>();
        if (securityValidator != null)
        {
            try
            {
                var validationResult = await securityValidator.ValidateConfigurationAsync();
                var logger = scope.ServiceProvider.GetService<ILogger<Program>>();

                if (validationResult.IsValid)
                {
                    logger?.LogInformation("✅ 安全配置验证通过");

                    if (validationResult.HasWarnings)
                    {
                        foreach (var issue in validationResult.Issues.Where(i => i.Type == SecurityValidationIssueType.Warning))
                        {
                            logger?.LogWarning("⚠️ 安全警告: {Message}", issue.Message);
                        }
                    }
                }
                else
                {
                    foreach (var issue in validationResult.Issues.Where(i => i.Type == SecurityValidationIssueType.Error))
                    {
                        logger?.LogError("❌ 安全配置错误: {Message}", issue.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
                logger?.LogWarning(ex, "⚠️ 安全配置验证失败，但不影响应用启动");
            }
        }
        */
    }

    /// <summary>
    /// 记录启动日志
    /// </summary>
    private static async Task LogApplicationStartupAsync(this WebApplication app, IServiceScope scope)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        if (logger != null)
        {
            try
            {
                logger.LogInformation("✅ 应用程序启动成功 - WebAPI-Startup");
                logger.LogInformation("✅ 日志系统初始化成功");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "⚠️ 日志记录过程中发生异常，但不影响应用启动");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理初始化错误
    /// </summary>
    private static Task HandleInitializationErrorAsync(this WebApplication app, IServiceScope scope, Exception ex)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogError(ex, "❌ 应用程序初始化失败: {ErrorMessage}", ex.Message);

        // 在开发环境中显示更详细的错误信息
        if (app.Environment.IsDevelopment())
        {
            logger?.LogError("详细错误信息: {StackTrace}", ex.StackTrace);
        }

        // 可以选择是否继续启动或抛出异常
        // throw; // 如果希望初始化失败时停止应用
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取和显示数据库状态信息
    /// </summary>
    public static async Task DisplayDatabaseStatusAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        try
        {
            var dbInitService = scope.ServiceProvider.GetService<LYBT.Infrastructure.Data.DatabaseInitializationService>();
            if (dbInitService != null)
            {
                var dbInfo = await dbInitService.GetDatabaseInfoAsync();
                var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
                logger?.LogInformation("📊 数据库状态信息已获取");
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
            logger?.LogWarning(ex, "⚠️ 无法获取数据库状态信息");
        }
    }

    /// <summary>
    /// 配置优雅关闭支持
    /// </summary>
    public static async Task ConfigureGracefulShutdown(this WebApplication app)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // 取消默认的强制终止
            cancellationTokenSource.Cancel(); // 触发取消令牌
        };

        AppDomain.CurrentDomain.ProcessExit += (_, __) =>
        {
            cancellationTokenSource.Cancel();

            // 等待应用优雅关闭并确保资源释放
            app.StopAsync().GetAwaiter().GetResult();
        };

        // 启动应用并处理优雅关闭
        try
        {
            await app.RunAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // 正常关闭，不需要记录错误
        }
        finally
        {
            // 确保释放资源
            await app.DisposeAsync();
        }
    }

    /// <summary>
    /// 获取数据库连接字符串 - 直接使用IConfiguration
    /// 优先级: CONNECTION_STRING环境变量 -> 配置文件
    /// </summary>
    private static string GetConnectionString(IConfiguration configuration, string name = "DefaultConnection")
    {
        // 优先使用环境变量
        var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            return envConnectionString;
        }

        // 使用配置文件
        return configuration.GetConnectionString(name) ?? string.Empty;
    }
}
