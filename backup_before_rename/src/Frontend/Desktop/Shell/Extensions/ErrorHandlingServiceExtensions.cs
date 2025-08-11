using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Logging;
using LYBT.Desktop.Core.Interfaces.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>
    /// 错误处理和日志服务注册扩展
    /// </summary>
    public static class ErrorHandlingServiceExtensions
    {
        /// <summary>
        /// 注册错误处理和日志服务
        /// </summary>
        public static IContainerRegistry RegisterErrorHandlingAndLogging(this IContainerRegistry container)
        {
            // 1. 配置Serilog
            ConfigureSerilog();
            
            // 2. 注册日志工厂
            RegisterLoggingServices(container);
            
            // 3. 注册错误处理服务
            RegisterErrorHandlingServices(container);
            
            // 4. 注册通知服务
            RegisterNotificationServices(container);
            
            return container;
        }
        
        private static void ConfigureSerilog()
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LYBT", "Logs", "lybt-.log");
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                // 以下enrichers需要额外的NuGet包
                // .Enrich.WithThreadId() // 需要 Serilog.Enrichers.Thread 包
                // .Enrich.WithProcessId() // 需要 Serilog.Enrichers.Process 包
                // .Enrich.WithMachineName() // 需要 Serilog.Enrichers.Environment 包
                // .Enrich.WithEnvironmentUserName() // 需要 Serilog.Enrichers.Environment 包
                // 控制台输出（开发环境）
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                // 文件输出（JSON格式）
                .WriteTo.File(
                    new JsonFormatter(),
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10485760) // 10MB
                // 调试输出 (需要 Serilog.Sinks.Debug 包)
                // #if DEBUG
                // .WriteTo.Debug()
                // #endif
                // Windows事件日志（仅错误和严重）- 需要 Serilog.Sinks.EventLog 包
                // .WriteTo.EventLog("LYBT", 
                //     restrictedToMinimumLevel: LogEventLevel.Error)
                .CreateLogger();
        }
        
        private static void RegisterLoggingServices(IContainerRegistry container)
        {
            // 注册Serilog日志工厂
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
            });
                
            container.RegisterInstance<ILoggerFactory>(loggerFactory);
            container.Register(typeof(ILogger<>), typeof(Logger<>));
            
            // 注册日志上下文提供者
            container.RegisterSingleton<ILogContextProvider, LogContextProvider>();
            
            // 注册结构化日志服务
            container.RegisterSingleton<IStructuredLoggingService, StructuredLoggingService>();
        }
        
        private static void RegisterErrorHandlingServices(IContainerRegistry container)
        {
            // 注册错误分类器
            container.RegisterSingleton<IErrorClassifier, ErrorClassifier>();
            
            // 注册全局异常处理器
            container.RegisterSingleton<IGlobalExceptionHandler, GlobalExceptionHandler>();
            
            // 注册错误处理服务
            container.RegisterSingleton<IErrorHandlingService, Services.ErrorHandlingService>();
        }
        
        private static void RegisterNotificationServices(IContainerRegistry container)
        {
            // 注册用户通知服务
            container.RegisterSingleton<IUserNotificationService, UserNotificationService>();
        }
    }
    
    // ErrorHandlingServiceAdapter 临时禁用 - 需要实现完整的IErrorHandlingService接口
    // TODO: 完成接口实现或使用已有的ErrorHandlingService
    /*
    /// <summary>
    /// 错误处理服务适配器 - 兼容旧的IErrorHandlingService接口
    /// </summary>
    public class ErrorHandlingServiceAdapter : IErrorHandlingService
    {
        private readonly IGlobalExceptionHandler _globalExceptionHandler;
        private readonly IStructuredLoggingService _loggingService;
        private readonly IUserNotificationService _notificationService;
        
        public ErrorHandlingServiceAdapter(
            IGlobalExceptionHandler globalExceptionHandler,
            IStructuredLoggingService loggingService,
            IUserNotificationService notificationService)
        {
            _globalExceptionHandler = globalExceptionHandler;
            _loggingService = loggingService;
            _notificationService = notificationService;
        }
        
        public void RegisterGlobalExceptionHandlers()
        {
            _globalExceptionHandler.RegisterGlobalHandlers();
        }
        
        public async Task HandleExceptionAsync(Exception exception, string context = "")
        {
            _loggingService.LogError(exception, "错误上下文: {Context}", context);
            await _globalExceptionHandler.HandleExceptionAsync(exception, ExceptionSource.Manual);
        }
        
        public void LogError(string message, Exception? exception = null)
        {
            if (exception != null)
            {
                _loggingService.LogError(exception, message);
            }
            else
            {
                _loggingService.LogError(null, message);
            }
        }
        
        public void LogWarning(string message)
        {
            _loggingService.LogWarning(message);
        }
        
        public void LogInfo(string message)
        {
            _loggingService.LogInformation(message);
        }
        
        public async Task ShowErrorAsync(string message)
        {
            await _notificationService.ShowErrorAsync(message, LYBT.Desktop.Core.Exceptions.ErrorSeverity.Error);
        }
    }
    */
}