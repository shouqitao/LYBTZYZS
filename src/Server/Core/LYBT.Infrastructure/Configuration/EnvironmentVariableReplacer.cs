using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace LYBT.Infrastructure.Configuration
{
    /// <summary>
    /// 环境变量替换器接口
    /// </summary>
    public interface IEnvironmentVariableReplacer
    {
        /// <summary>
        /// 替换配置中的环境变量占位符
        /// </summary>
        void ReplaceEnvironmentVariables(IConfigurationBuilder configurationBuilder);

        /// <summary>
        /// 替换字符串中的环境变量占位符
        /// </summary>
        string ReplaceVariables(string input);

        /// <summary>
        /// 加载.env文件
        /// </summary>
        void LoadDotEnvFile(string filePath);
    }

    /// <summary>
    /// 环境变量替换器实现
    /// UltraThink v2.0: 支持自动替换配置中的环境变量占位符
    /// </summary>
    public class EnvironmentVariableReplacer : IEnvironmentVariableReplacer
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<EnvironmentVariableReplacer> _logger;
        private readonly Dictionary<string, string> _environmentVariables;
        private readonly Regex _variablePattern = new Regex(@"\$\{([^}]+)\}", RegexOptions.Compiled);

        public EnvironmentVariableReplacer(IHostEnvironment hostEnvironment, ILogger<EnvironmentVariableReplacer>? logger = null)
        {
            _hostEnvironment = hostEnvironment;
            _logger = logger ?? CreateNullLogger();
            _environmentVariables = new Dictionary<string, string>();

            // 初始化时加载.env文件
            InitializeEnvironmentVariables();
        }

        /// <summary>
        /// 替换配置构建器中的环境变量
        /// </summary>
        public void ReplaceEnvironmentVariables(IConfigurationBuilder configurationBuilder)
        {
            try
            {
                _logger.LogInformation("开始替换配置中的环境变量占位符");

                // 加载.env文件到环境变量字典
                LoadDotEnvFromContentRoot();

                // 构建临时配置以获取当前值
                var tempConfig = configurationBuilder.Build();
                var replacements = new Dictionary<string, string>();

                // 遍历所有配置项，替换其中的环境变量占位符
                foreach (var kvp in tempConfig.AsEnumerable())
                {
                    if (kvp.Value != null && _variablePattern.IsMatch(kvp.Value))
                    {
                        var replacedValue = ReplaceVariables(kvp.Value);
                        if (replacedValue != kvp.Value)
                        {
                            replacements[kvp.Key] = replacedValue;
                            _logger.LogDebug("替换配置项 {Key}: {Original} -> {Replaced}", 
                                kvp.Key, MaskSensitiveValue(kvp.Value), MaskSensitiveValue(replacedValue));
                        }
                    }
                }

                // 应用替换
                if (replacements.Any())
                {
                    configurationBuilder.AddInMemoryCollection(replacements);
                    _logger.LogInformation("成功替换 {Count} 个配置项中的环境变量占位符", replacements.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "替换环境变量时发生错误");
                throw new InvalidOperationException("环境变量替换失败", ex);
            }
        }

        /// <summary>
        /// 替换字符串中的环境变量占位符
        /// </summary>
        public string ReplaceVariables(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return _variablePattern.Replace(input, match =>
            {
                var variableName = match.Groups[1].Value;
                
                // 优先从本地环境变量字典查找
                if (_environmentVariables.TryGetValue(variableName, out var value))
                {
                    return value;
                }

                // 然后从系统环境变量查找
                value = Environment.GetEnvironmentVariable(variableName);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }

                // 如果是生产环境且找不到关键变量，记录警告
                if (_hostEnvironment.IsProduction() && IsCriticalVariable(variableName))
                {
                    _logger.LogWarning("生产环境中未找到关键环境变量: {VariableName}", variableName);
                }

                // 返回原始占位符（未找到替换值）
                return match.Value;
            });
        }

        /// <summary>
        /// 加载.env文件
        /// </summary>
        public void LoadDotEnvFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning(".env文件不存在: {FilePath}", filePath);
                return;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                var loadedCount = 0;

                foreach (var line in lines)
                {
                    if (ProcessEnvLine(line))
                    {
                        loadedCount++;
                    }
                }

                _logger.LogInformation("成功从 {FilePath} 加载 {Count} 个环境变量", filePath, loadedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载.env文件失败: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 初始化环境变量
        /// </summary>
        private void InitializeEnvironmentVariables()
        {
            // 首先从系统环境变量加载
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                if (entry.Key is string key && entry.Value is string value)
                {
                    _environmentVariables[key] = value;
                }
            }

            _logger.LogDebug("初始化完成，加载了 {Count} 个系统环境变量", _environmentVariables.Count);
        }

        /// <summary>
        /// 从内容根目录加载.env文件
        /// </summary>
        private void LoadDotEnvFromContentRoot()
        {
            var envFilePaths = new[]
            {
                Path.Combine(_hostEnvironment.ContentRootPath, ".env"),
                Path.Combine(_hostEnvironment.ContentRootPath, $".env.{_hostEnvironment.EnvironmentName.ToLower()}")
            };

            foreach (var filePath in envFilePaths)
            {
                if (File.Exists(filePath))
                {
                    LoadDotEnvFile(filePath);
                }
            }
        }

        /// <summary>
        /// 处理.env文件中的一行
        /// </summary>
        private bool ProcessEnvLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                return false;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                return false;

            var key = line.Substring(0, separatorIndex).Trim();
            var value = line.Substring(separatorIndex + 1).Trim();

            // 移除引号包装
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
            {
                value = value.Substring(1, value.Length - 2);
            }
            else if (value.StartsWith("'") && value.EndsWith("'") && value.Length >= 2)
            {
                value = value.Substring(1, value.Length - 2);
            }

            // 存储到本地字典
            _environmentVariables[key] = value;

            // 也设置到系统环境变量（进程级别）
            Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);

            return true;
        }

        /// <summary>
        /// 判断是否为关键变量
        /// </summary>
        private static bool IsCriticalVariable(string variableName)
        {
            var criticalVariables = new[]
            {
                "JWT_SECRET",
                "ADMIN_DEFAULT_PASSWORD", 
                "USER_DEFAULT_PASSWORD",
                "DB_CONNECTION_STRING",
                "DATA_ENCRYPTION_KEY"
            };

            return criticalVariables.Contains(variableName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 遮罩敏感值用于日志记录
        /// </summary>
        private static string MaskSensitiveValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // 如果包含敏感关键词，进行遮罩
            var sensitivePatterns = new[] { "password", "secret", "key", "token" };
            if (sensitivePatterns.Any(pattern => value.ToLower().Contains(pattern)))
            {
                return value.Length <= 8 ? "****" : $"{value.Substring(0, 4)}****{value.Substring(value.Length - 4)}";
            }

            return value;
        }

        /// <summary>
        /// 创建空的日志记录器（用于可选依赖）
        /// </summary>
        private static ILogger<EnvironmentVariableReplacer> CreateNullLogger()
        {
            return new Microsoft.Extensions.Logging.Abstractions.NullLogger<EnvironmentVariableReplacer>();
        }
    }

    /// <summary>
    /// 环境变量替换器扩展方法
    /// </summary>
    public static class EnvironmentVariableReplacerExtensions
    {
        /// <summary>
        /// 添加环境变量替换支持
        /// </summary>
        public static IConfigurationBuilder AddEnvironmentVariableReplacement(
            this IConfigurationBuilder builder, 
            IHostEnvironment hostEnvironment)
        {
            var replacer = new EnvironmentVariableReplacer(hostEnvironment);
            replacer.ReplaceEnvironmentVariables(builder);
            return builder;
        }

        /// <summary>
        /// 添加.env文件支持（带环境变量替换）
        /// </summary>
        public static IConfigurationBuilder AddDotEnvFile(
            this IConfigurationBuilder builder,
            IHostEnvironment hostEnvironment,
            string? fileName = null)
        {
            fileName ??= ".env";
            var filePath = Path.Combine(hostEnvironment.ContentRootPath, fileName);
            
            var replacer = new EnvironmentVariableReplacer(hostEnvironment);
            replacer.LoadDotEnvFile(filePath);
            replacer.ReplaceEnvironmentVariables(builder);
            
            return builder;
        }
    }
}