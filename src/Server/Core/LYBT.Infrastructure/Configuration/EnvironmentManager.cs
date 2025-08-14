using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration
{
    /// <summary>
    /// 环境管理器接口
    /// </summary>
    public interface IEnvironmentManager
    {
        /// <summary>
        /// 获取环境变量
        /// </summary>
        string GetEnvironmentVariable(string key, string defaultValue = "");

        /// <summary>
        /// 设置环境变量
        /// </summary>
        void SetEnvironmentVariable(string key, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process);

        /// <summary>
        /// 获取所有环境变量
        /// </summary>
        Dictionary<string, string> GetAllEnvironmentVariables();

        /// <summary>
        /// 验证环境配置
        /// </summary>
        ValidationResult ValidateEnvironment();

        /// <summary>
        /// 加载环境配置文件
        /// </summary>
        void LoadEnvironmentFile(string filePath);

        /// <summary>
        /// 当前环境信息
        /// </summary>
        EnvironmentInfo GetEnvironmentInfo();
    }

    /// <summary>
    /// 环境信息
    /// </summary>
    public class EnvironmentInfo
    {
        /// <summary>
        /// 环境名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否为开发环境
        /// </summary>
        public bool IsDevelopment { get; set; }

        /// <summary>
        /// 是否为生产环境
        /// </summary>
        public bool IsProduction { get; set; }

        /// <summary>
        /// 是否为测试环境
        /// </summary>
        public bool IsTest { get; set; }

        /// <summary>
        /// 机器名称
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 运行时版本
        /// </summary>
        public string RuntimeVersion { get; set; } = string.Empty;

        /// <summary>
        /// 应用程序版本
        /// </summary>
        public string ApplicationVersion { get; set; } = string.Empty;
    }

    /// <summary>
    /// 环境管理器实现
    /// </summary>
    public class EnvironmentManager : IEnvironmentManager
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;

        public EnvironmentManager(IHostEnvironment hostEnvironment, IConfiguration configuration)
        {
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// 获取环境变量
        /// </summary>
        public string GetEnvironmentVariable(string key, string defaultValue = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return defaultValue;

            // 支持多种环境变量格式
            var candidates = new[]
            {
                key,                          // 原始键名
                key.ToUpperInvariant(),      // 大写
                key.Replace(":", "__"),      // ASP.NET Core 格式
                $"LYBT_{key}",              // 带前缀
                $"LYBT_{key.ToUpperInvariant()}" // 带前缀大写
            };

            foreach (var candidate in candidates)
            {
                var value = Environment.GetEnvironmentVariable(candidate);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 设置环境变量
        /// </summary>
        public void SetEnvironmentVariable(string key, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            Environment.SetEnvironmentVariable(key, value, target);
        }

        /// <summary>
        /// 获取所有环境变量
        /// </summary>
        public Dictionary<string, string> GetAllEnvironmentVariables()
        {
            var variables = new Dictionary<string, string>();
            
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                if (entry.Key is string key && entry.Value is string value)
                {
                    variables[key] = value;
                }
            }

            return variables;
        }

        /// <summary>
        /// 验证环境配置
        /// </summary>
        public ValidationResult ValidateEnvironment()
        {
            var errors = new List<string>();

            try
            {
                // 验证必需的环境变量
                var requiredVariables = GetRequiredEnvironmentVariables();
                foreach (var variable in requiredVariables)
                {
                    var value = GetEnvironmentVariable(variable);
                    if (string.IsNullOrEmpty(value))
                    {
                        errors.Add($"必需的环境变量 '{variable}' 未设置");
                    }
                }

                // 验证环境特定配置
                if (_hostEnvironment.IsProduction())
                {
                    ValidateProductionEnvironment(errors);
                }
                else if (_hostEnvironment.IsDevelopment())
                {
                    ValidateDevelopmentEnvironment(errors);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"环境验证异常: {ex.Message}");
            }

            if (errors.Any())
            {
                return new ValidationResult(string.Join("; ", errors));
            }

            return ValidationResult.Success!;
        }

        /// <summary>
        /// 加载环境配置文件（.env文件）
        /// </summary>
        public void LoadEnvironmentFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                if (_hostEnvironment.IsDevelopment())
                {
                    // 开发环境创建示例文件
                    CreateSampleEnvironmentFile(filePath);
                }
                return;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim().Trim('"');
                        SetEnvironmentVariable(key, value);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"加载环境文件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取环境信息
        /// </summary>
        public EnvironmentInfo GetEnvironmentInfo()
        {
            return new EnvironmentInfo
            {
                Name = _hostEnvironment.EnvironmentName,
                IsDevelopment = _hostEnvironment.IsDevelopment(),
                IsProduction = _hostEnvironment.IsProduction(),
                IsTest = _hostEnvironment.IsEnvironment("Test") || _hostEnvironment.IsEnvironment("Testing"),
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                RuntimeVersion = Environment.Version.ToString(),
                ApplicationVersion = GetApplicationVersion()
            };
        }

        /// <summary>
        /// 获取必需的环境变量
        /// </summary>
        private List<string> GetRequiredEnvironmentVariables()
        {
            var required = new List<string>();

            if (_hostEnvironment.IsProduction())
            {
                required.AddRange(new[]
                {
                    "JWT_SECRET",
                    "ADMIN_DEFAULT_PASSWORD",
                    "USER_DEFAULT_PASSWORD",
                    "ASPNETCORE_ENVIRONMENT"
                });
            }

            return required;
        }

        /// <summary>
        /// 验证生产环境配置
        /// </summary>
        private void ValidateProductionEnvironment(List<string> errors)
        {
            // 验证安全相关环境变量
            var jwtSecret = GetEnvironmentVariable("JWT_SECRET");
            if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
            {
                errors.Add("生产环境JWT_SECRET长度不足32个字符");
            }

            // 验证密码相关
            var adminPassword = GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
            if (string.IsNullOrEmpty(adminPassword) || adminPassword.Length < 12)
            {
                errors.Add("生产环境管理员默认密码强度不足");
            }

            // 验证数据库连接
            var dbConnection = GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (string.IsNullOrEmpty(dbConnection))
            {
                errors.Add("生产环境数据库连接字符串未设置");
            }
        }

        /// <summary>
        /// 验证开发环境配置
        /// </summary>
        private void ValidateDevelopmentEnvironment(List<string> errors)
        {
            // 开发环境的验证相对宽松
            var envFile = Path.Combine(_hostEnvironment.ContentRootPath, ".env");
            if (!File.Exists(envFile))
            {
                errors.Add("开发环境建议创建.env文件进行本地配置");
            }
        }

        /// <summary>
        /// 创建示例环境文件
        /// </summary>
        private void CreateSampleEnvironmentFile(string filePath)
        {
            var sampleContent = @"# 凌隐宝堂中医诊所系统 - 环境配置文件
# 复制此文件为 .env 并根据实际情况修改配置值

# 数据库配置
# ConnectionStrings__DefaultConnection=Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true

# JWT配置
# JWT_SECRET=请替换为至少32个字符的强密钥

# 默认密码配置
# ADMIN_DEFAULT_PASSWORD=Admin@123456
# USER_DEFAULT_PASSWORD=ChangeMe123

# 环境配置
ASPNETCORE_ENVIRONMENT=Development

# 日志配置
# ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Information

# 其他配置
# ASPNETCORE_URLS=https://localhost:7001;http://localhost:5001
";

            try
            {
                File.WriteAllText(filePath + ".example", sampleContent);
            }
            catch
            {
                // 忽略文件创建错误
            }
        }

        /// <summary>
        /// 获取应用程序版本
        /// </summary>
        private string GetApplicationVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                var version = assembly?.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}