using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LYBT.Infrastructure.Configuration
{

    /// <summary>
    /// 简化配置服务 - UltraThink配置管理重构
    /// 替代过度复杂的ConfigurationManager/EnvironmentManager/SecretManager
    /// 使用.NET内置机制，专注小诊所实际需求
    /// </summary>
    public interface ISimplifiedConfigurationService
    {

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        string GetConnectionString(string name = "DefaultConnection");

        /// <summary>
        /// 获取配置节
        /// </summary>
        T GetSection<T>(string sectionName) where T : class, new();

        /// <summary>
        /// 是否为开发环境
        /// </summary>
        bool IsDevelopment { get; }

        /// <summary>
        /// 是否为生产环境
        /// </summary>
        bool IsProduction { get; }

        /// <summary>
        /// 获取JWT秘钥（从环境变量或配置文件）
        /// </summary>
        string GetJwtSecret();

        /// <summary>
        /// 获取管理员密码（从环境变量或配置文件）
        /// </summary>
        string GetAdminPassword();

        /// <summary>
        /// 获取用户默认密码（从环境变量或配置文件）
        /// </summary>
        string GetUserDefaultPassword();
    }

    /// <summary>
    /// 简化配置服务实现
    /// </summary>
    public class SimplifiedConfigurationService : ISimplifiedConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public SimplifiedConfigurationService(IConfiguration configuration, IHostEnvironment environment)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// 获取数据库连接字符串
        /// 支持环境变量覆盖：CONNECTION_STRING
        /// </summary>
        public string GetConnectionString(string name = "DefaultConnection")
        {
            // 优先使用环境变量
            var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (!string.IsNullOrEmpty(envConnectionString))
            {
                return envConnectionString;
            }

            // 使用配置文件
            return _configuration.GetConnectionString(name) ?? string.Empty;
        }

        /// <summary>
        /// 获取配置节
        /// </summary>
        public T GetSection<T>(string sectionName) where T : class, new()
        {
            var section = _configuration.GetSection(sectionName);
            var config = new T();
            section.Bind(config);
            return config;
        }

        /// <summary>
        /// 是否为开发环境
        /// </summary>
        public bool IsDevelopment => _environment.IsDevelopment();

        /// <summary>
        /// 是否为生产环境
        /// </summary>
        public bool IsProduction => _environment.IsProduction();

        /// <summary>
        /// 获取JWT秘钥
        /// 优先级: JWT_SECRET环境变量 -> 配置文件
        /// </summary>
        public string GetJwtSecret()
        {
            // 优先使用环境变量
            var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (!string.IsNullOrEmpty(envSecret))
            {
                return envSecret;
            }

            // 使用配置文件
            var configSecret = _configuration["JwtOptions:Secret"];
            if (!string.IsNullOrEmpty(configSecret) && !configSecret.Contains("${"))
            {
                return configSecret;
            }

            // 开发环境允许使用默认值
            if (IsDevelopment)
            {
                return "UltraThink-LYBT-Development-Secret-Key-2025-09-02-Very-Long-Secret-For-JWT-Signing";
            }

            throw new InvalidOperationException("JWT秘钥未配置：请设置JWT_SECRET环境变量或配置文件中的JwtOptions:Secret");
        }

        /// <summary>
        /// 获取管理员密码
        /// 优先级: ADMIN_DEFAULT_PASSWORD环境变量 -> 配置文件
        /// </summary>
        public string GetAdminPassword()
        {
            // 优先使用环境变量
            var envPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
            if (!string.IsNullOrEmpty(envPassword))
            {
                return envPassword;
            }

            // 使用配置文件
            var configPassword = _configuration["SysAdminOptions:DefaultPassword"];
            if (!string.IsNullOrEmpty(configPassword))
            {
                return configPassword;
            }

            throw new InvalidOperationException("管理员密码未配置：请设置ADMIN_DEFAULT_PASSWORD环境变量或配置文件中的SysAdminOptions:DefaultPassword");
        }

        /// <summary>
        /// 获取用户默认密码
        /// 优先级: USER_DEFAULT_PASSWORD环境变量 -> 配置文件
        /// </summary>
        public string GetUserDefaultPassword()
        {
            // 优先使用环境变量
            var envPassword = Environment.GetEnvironmentVariable("USER_DEFAULT_PASSWORD");
            if (!string.IsNullOrEmpty(envPassword))
            {
                return envPassword;
            }

            // 使用配置文件
            var configPassword = _configuration["UserOptions:DefaultUserPassword"];
            if (!string.IsNullOrEmpty(configPassword))
            {
                return configPassword;
            }

            throw new InvalidOperationException("用户默认密码未配置：请设置USER_DEFAULT_PASSWORD环境变量或配置文件中的UserOptions:DefaultUserPassword");
        }
    }
}
