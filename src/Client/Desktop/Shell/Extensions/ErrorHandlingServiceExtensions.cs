using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Extensions {

    /// <summary>
    /// 错误处理和日志服务注册扩展 - UltraThink v2.0简化版
    /// </summary>
    public static class ErrorHandlingServiceExtensions {

        /// <summary>
        /// 注册错误处理和日志服务
        /// </summary>
        public static IContainerRegistry RegisterErrorHandlingAndLogging(this IContainerRegistry container) {
            RegisterLoggingServices(container);
            return container;
        }

        private static void RegisterLoggingServices(IContainerRegistry container) {
            // UltraThink v2.0: 使用标准Microsoft.Extensions.Logging
            var loggerFactory = LoggerFactory.Create(builder => {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            container.RegisterInstance<ILoggerFactory>(loggerFactory);
            container.Register(typeof(ILogger<>), typeof(Logger<>));
        }
    }
}
