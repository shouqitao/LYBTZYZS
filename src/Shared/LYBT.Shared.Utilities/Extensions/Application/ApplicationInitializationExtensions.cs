using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Utilities.Extensions.Application
{
    /// <summary>
    /// 应用程序初始化扩展方法
    /// </summary>
    public static class ApplicationInitializationExtensions
    {
        /// <summary>
        /// 验证关键配置项
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <param name="environment">环境名称</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <returns>验证结果</returns>
        public static ConfigurationValidationResult ValidateCriticalConfiguration(
            IConfiguration configuration,
            string? environment = null,
            ILogger? logger = null)
        {
            var result = new ConfigurationValidationResult();
            var env = environment ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // 验证数据库连接字符串
            var connectionString = GetConnectionString(configuration);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                result.AddError("数据库连接字符串未配置");
                logger?.LogError("数据库连接字符串未配置");
            }
            else
            {
                logger?.LogInformation("✅ 数据库连接配置验证通过");
            }

            // 验证JWT配置
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                           configuration["JwtOptions:Secret"];

            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                if (env.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError("生产环境必须配置JWT密钥");
                    logger?.LogError("生产环境必须配置JWT密钥");
                }
                else
                {
                    result.AddWarning("JWT密钥未配置，使用默认开发密钥");
                    logger?.LogWarning("JWT密钥未配置，使用默认开发密钥");
                }
            }
            else
            {
                logger?.LogInformation("✅ JWT配置验证通过");
            }

            return result;
        }

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <param name="name">连接字符串名称</param>
        /// <returns>连接字符串</returns>
        public static string GetConnectionString(IConfiguration configuration, string name = "DefaultConnection")
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

        /// <summary>
        /// 显示应用程序启动信息
        /// </summary>
        /// <param name="environment">环境名称</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="additionalInfo">额外信息</param>
        public static void LogApplicationStartup(
            string environment,
            ILogger logger,
            Dictionary<string, string>? additionalInfo = null)
        {
            logger.LogInformation("✅ 应用程序启动成功");
            logger.LogInformation("🌍 运行环境: {Environment}, 机器: {MachineName}",
                environment, Environment.MachineName);

            if (additionalInfo != null)
            {
                foreach (var (key, value) in additionalInfo)
                {
                    logger.LogInformation("📊 {Key}: {Value}", key, value);
                }
            }
        }

        /// <summary>
        /// 配置优雅关闭支持
        /// </summary>
        /// <param name="lifetime">应用程序生命周期</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="shutdownTimeout">关闭超时时间（秒）</param>
        public static void ConfigureGracefulShutdown(
            IHostApplicationLifetime lifetime,
            ILogger logger,
            int shutdownTimeout = 30)
        {
            lifetime.ApplicationStarted.Register(() =>
            {
                logger.LogInformation("应用程序已启动");
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                logger.LogInformation("应用程序正在停止，执行优雅关闭...");
                // 这里可以添加清理逻辑
                Thread.Sleep(TimeSpan.FromSeconds(Math.Min(shutdownTimeout, 60)));
            });

            lifetime.ApplicationStopped.Register(() =>
            {
                logger.LogInformation("应用程序已停止");
            });
        }
    }

    /// <summary>
    /// 配置验证结果
    /// </summary>
    public class ConfigurationValidationResult
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// 是否有警告
        /// </summary>
        public bool HasWarnings => _warnings.Count > 0;

        /// <summary>
        /// 错误列表
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// 警告列表
        /// </summary>
        public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        /// <summary>
        /// 添加错误
        /// </summary>
        public void AddError(string error)
        {
            _errors.Add(error);
        }

        /// <summary>
        /// 添加警告
        /// </summary>
        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }
    }
}