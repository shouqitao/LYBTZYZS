using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Serilog;

namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>日志服务注册扩展方法</summary>
    public static class LoggingRegistrationExtensions
    {
        /// <summary>注册所有日志服务</summary>
        public static void RegisterLogging(this IContainerRegistry containerRegistry)
        {
            // LoggerFactory 单例
            containerRegistry.RegisterSingleton<ILoggerFactory>(() =>
                LoggerFactory.Create(builder => builder.AddSerilog(dispose: false)));

            // 开放泛型注册: 任意 ILogger<T> 由容器自动解析为 Logger<T>
            // 无需手动注册具体类型的 ILogger<T> 单例
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
        }
    }
}
