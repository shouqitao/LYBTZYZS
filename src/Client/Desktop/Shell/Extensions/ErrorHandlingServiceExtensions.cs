using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using LYBT.Desktop.Core.Services;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>
    /// 错误处理和日志服务注册扩展
    /// </summary>
    public static class ErrorHandlingServiceExtensions
    {
        /// <summary>
        /// 注册错误处理和日志服务 - UltraThink简化版
        /// </summary>
        public static IContainerRegistry RegisterErrorHandlingAndLogging(this IContainerRegistry container)
        {
            // UltraThink重构：删除复杂的Serilog配置，使用标准Microsoft.Extensions.Logging
            
            // 1. 注册标准日志服务
            RegisterLoggingServices(container);
            
            // 2. 注册错误处理服务
            RegisterErrorHandlingServices(container);
            
            // 3. 注册通知服务
            RegisterNotificationServices(container);
            
            return container;
        }
        
        // UltraThink重构：删除复杂的Serilog配置方法
        
        private static void RegisterLoggingServices(IContainerRegistry container)
        {
            // UltraThink重构：使用标准Microsoft.Extensions.Logging（WPF应用只使用Debug日志）
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
                
            container.RegisterInstance<ILoggerFactory>(loggerFactory);
            container.Register(typeof(ILogger<>), typeof(Logger<>));
            
            // UltraThink重构：删除复杂的结构化日志服务，使用标准ILogger
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
    
    // UltraThink重构：删除复杂的适配器代码，使用标准服务实现
}