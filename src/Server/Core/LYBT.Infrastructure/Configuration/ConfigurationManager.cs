using LYBT.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LYBT.Infrastructure.Configuration
{
    /// <summary>
    /// 统一配置管理实现类
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public ConfigurationManager(IConfiguration configuration, IHostEnvironment environment)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
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
        /// 当前环境名称
        /// </summary>
        public string Environment => _environment.EnvironmentName;

        /// <summary>
        /// 获取配置节
        /// </summary>
        public T GetSection<T>(string sectionName) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentNullException(nameof(sectionName));

            var section = _configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                throw new InvalidOperationException($"配置节 '{sectionName}' 不存在");
            }

            var config = new T();
            section.Bind(config);
            
            // 处理环境变量替换
            ProcessEnvironmentVariables(config);
            
            return config;
        }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        public string GetConnectionString(string name = "DefaultConnection")
        {
            var connectionString = _configuration.GetConnectionString(name);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"连接字符串 '{name}' 未配置");
            }

            return ProcessEnvironmentVariableString(connectionString);
        }

        /// <summary>
        /// 获取配置值，支持环境变量
        /// </summary>
        public string GetValue(string key, string defaultValue = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return defaultValue;

            var value = _configuration[key];
            if (string.IsNullOrWhiteSpace(value))
                value = defaultValue;

            return ProcessEnvironmentVariableString(value);
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        public ValidationResult ValidateConfiguration()
        {
            var errors = new List<string>();

            try
            {
                // 验证核心配置
                ValidateConnectionString(errors);
                ValidateJwtOptions(errors);
                ValidateAuthOptions(errors);
                ValidateSecurityConfiguration(errors);
            }
            catch (Exception ex)
            {
                errors.Add($"配置验证异常: {ex.Message}");
            }

            if (errors.Any())
            {
                return new ValidationResult(string.Join("; ", errors));
            }

            return ValidationResult.Success!;
        }

        /// <summary>
        /// 处理对象中的环境变量替换
        /// </summary>
        private void ProcessEnvironmentVariables(object obj)
        {
            if (obj == null) return;

            var properties = obj.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p.CanWrite);

            foreach (var property in properties)
            {
                var value = property.GetValue(obj) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    var processedValue = ProcessEnvironmentVariableString(value);
                    property.SetValue(obj, processedValue);
                }
            }
        }

        /// <summary>
        /// 处理字符串中的环境变量替换
        /// </summary>
        private string ProcessEnvironmentVariableString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // 替换 ${VAR_NAME} 格式的环境变量
            var pattern = @"\$\{([^}]+)\}";
            return System.Text.RegularExpressions.Regex.Replace(value, pattern, match =>
            {
                var envVarName = match.Groups[1].Value;
                var envValue = System.Environment.GetEnvironmentVariable(envVarName);
                
                if (string.IsNullOrEmpty(envValue))
                {
                    // 如果是生产环境且环境变量为空，抛出异常
                    if (IsProduction)
                    {
                        throw new InvalidOperationException($"生产环境中环境变量 '{envVarName}' 未设置");
                    }
                    
                    // 开发环境返回原始值
                    return match.Value;
                }
                
                return envValue;
            });
        }

        /// <summary>
        /// 验证连接字符串
        /// </summary>
        private void ValidateConnectionString(List<string> errors)
        {
            try
            {
                var connectionString = GetConnectionString();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    errors.Add("数据库连接字符串为空");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"连接字符串配置错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证JWT配置
        /// </summary>
        private void ValidateJwtOptions(List<string> errors)
        {
            try
            {
                var jwtSection = _configuration.GetSection("JwtOptions");
                if (!jwtSection.Exists())
                {
                    errors.Add("JwtOptions配置节缺失");
                    return;
                }

                var secret = ProcessEnvironmentVariableString(jwtSection["Secret"] ?? "");
                if (string.IsNullOrWhiteSpace(secret))
                {
                    errors.Add("JWT密钥未配置");
                }
                else if (secret.Length < 32)
                {
                    errors.Add("JWT密钥长度不足32个字符");
                }

                var issuer = jwtSection["Issuer"];
                if (string.IsNullOrWhiteSpace(issuer))
                {
                    errors.Add("JWT签发者未配置");
                }

                var audience = jwtSection["Audience"];
                if (string.IsNullOrWhiteSpace(audience))
                {
                    errors.Add("JWT受众未配置");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"JWT配置验证错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证认证配置
        /// </summary>
        private void ValidateAuthOptions(List<string> errors)
        {
            try
            {
                var authSection = _configuration.GetSection("AuthOptions");
                if (authSection.Exists())
                {
                    var maxAttempts = authSection.GetValue<int>("MaxFailedLoginAttempts");
                    if (maxAttempts <= 0 || maxAttempts > 10)
                    {
                        errors.Add("最大登录失败次数应在1-10之间");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"认证配置验证错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证安全配置
        /// </summary>
        private void ValidateSecurityConfiguration(List<string> errors)
        {
            if (IsProduction)
            {
                var securitySection = _configuration.GetSection("Security");
                if (!securitySection.Exists())
                {
                    errors.Add("生产环境必须配置Security节");
                    return;
                }

                var httpsRequired = securitySection.GetValue<bool>("Https:RequireHttps");
                if (!httpsRequired)
                {
                    errors.Add("生产环境必须启用HTTPS");
                }

                var hideServerInfo = securitySection.GetValue<bool>("Environment:HideServerInfo");
                if (!hideServerInfo)
                {
                    errors.Add("生产环境应隐藏服务器信息");
                }
            }
        }
    }
}