using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Logging;
using LYBT.WebAPI.Services;

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
            await app.InitializeConfigurationServicesAsync(scope);
            
            // 3. 安全配置验证
            await app.ValidateSecurityConfigurationAsync(scope);
            
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
            var dbInitService = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Database.DatabaseInitializationService>();
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
    private static async Task InitializeConfigurationServicesAsync(this WebApplication app, IServiceScope scope)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();

        // =========== 统一配置管理验证 ===========
        try
        {
            // 验证所有配置
            scope.ServiceProvider.ValidateAllConfigurations();
            logger?.LogInformation("✅ 统一配置验证通过");

            // 验证环境配置
            var environmentManager = scope.ServiceProvider.GetService<IEnvironmentManager>();
            if (environmentManager != null)
            {
                var envValidation = environmentManager.ValidateEnvironment();
                if (envValidation == System.ComponentModel.DataAnnotations.ValidationResult.Success)
                {
                    logger?.LogInformation("✅ 环境配置验证通过");
                }
                else
                {
                    logger?.LogWarning("⚠️ 环境配置验证警告: {ValidationResult}", envValidation.ErrorMessage);
                }

                // 显示环境信息
                var envInfo = environmentManager.GetEnvironmentInfo();
                logger?.LogInformation("🌍 运行环境: {Environment}, 机器: {MachineName}, 版本: {Version}",
                    envInfo.Name, envInfo.MachineName, envInfo.ApplicationVersion);
            }

            // 验证秘钥完整性
            var secretManager = scope.ServiceProvider.GetService<ISecretManager>();
            if (secretManager != null)
            {
                if (secretManager.ValidateSecrets())
                {
                    logger?.LogInformation("✅ 秘钥验证通过");
                }
                else
                {
                    logger?.LogWarning("⚠️ 秘钥验证失败，某些功能可能受限");
                }
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

        // =========== 初始化统一配置服务 ===========
        var configService = scope.ServiceProvider.GetService<IUnifiedConfigService>();
        if (configService != null)
        {
            try
            {
                await configService.InitializeDefaultGlobalSettingsAsync();
                logger?.LogInformation("✅ 统一配置服务初始化成功");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "⚠️ 统一配置服务初始化失败，但不影响应用启动");
            }
        }
    }

    /// <summary>
    /// 安全配置验证
    /// </summary>
    private static async Task ValidateSecurityConfigurationAsync(this WebApplication app, IServiceScope scope)
    {
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
            var dbInitService = scope.ServiceProvider.GetService<LYBT.Infrastructure.Database.DatabaseInitializationService>();
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
}