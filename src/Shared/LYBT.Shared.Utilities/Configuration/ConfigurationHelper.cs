using Microsoft.Extensions.Configuration;

namespace LYBT.Shared.Utilities.Configuration
{
    /// <summary>
    /// 配置管理帮助类
    /// </summary>
    public static class ConfigurationHelper
    {
        /// <summary>
        /// 获取配置值（泛型）
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="configuration">配置对象</param>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public static T GetValue<T>(IConfiguration configuration, string key, T defaultValue = default!)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            try
            {
                // 处理不同类型的转换
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value;
                }

                if (typeof(T) == typeof(int))
                {
                    return (T)(object)int.Parse(value);
                }

                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)bool.Parse(value);
                }

                if (typeof(T) == typeof(double))
                {
                    return (T)(object)double.Parse(value);
                }

                if (typeof(T) == typeof(TimeSpan))
                {
                    return (T)(object)TimeSpan.Parse(value);
                }

                // 尝试使用类型转换器
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
                if (converter.CanConvertFrom(typeof(string)))
                {
                    return (T)converter.ConvertFromString(value)!;
                }

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <param name="name">连接字符串名称</param>
        /// <param name="environmentVariable">环境变量名称</param>
        /// <returns>连接字符串</returns>
        public static string GetConnectionString(
            IConfiguration configuration,
            string name = "DefaultConnection",
            string? environmentVariable = "CONNECTION_STRING")
        {
            // 优先使用环境变量
            if (!string.IsNullOrWhiteSpace(environmentVariable))
            {
                var envValue = Environment.GetEnvironmentVariable(environmentVariable);
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    return envValue;
                }
            }

            // 使用配置文件
            return configuration.GetConnectionString(name) ?? string.Empty;
        }

        /// <summary>
        /// 获取必需的配置值
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <param name="key">配置键</param>
        /// <returns>配置值</returns>
        /// <exception cref="InvalidOperationException">配置值不存在时抛出异常</exception>
        public static string GetRequiredValue(IConfiguration configuration, string key)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"配置项 '{key}' 未设置或为空");
            }
            return value;
        }

        /// <summary>
        /// 检查配置项是否存在
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <param name="key">配置键</param>
        /// <returns>是否存在</returns>
        public static bool Exists(IConfiguration configuration, string key)
        {
            return !string.IsNullOrWhiteSpace(configuration[key]);
        }

        /// <summary>
        /// 获取配置节
        /// </summary>
        /// <typeparam name="T">配置节类型</typeparam>
        /// <param name="configuration">配置对象</param>
        /// <param name="sectionName">节名称</param>
        /// <returns>配置节对象</returns>
        public static T? GetSection<T>(IConfiguration configuration, string sectionName) where T : class, new()
        {
            var section = configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                return null;
            }

            var result = new T();
            section.Bind(result);
            return result;
        }

        /// <summary>
        /// 获取配置节（必需）
        /// </summary>
        /// <typeparam name="T">配置节类型</typeparam>
        /// <param name="configuration">配置对象</param>
        /// <param name="sectionName">节名称</param>
        /// <returns>配置节对象</returns>
        /// <exception cref="InvalidOperationException">配置节不存在时抛出异常</exception>
        public static T GetRequiredSection<T>(IConfiguration configuration, string sectionName) where T : class, new()
        {
            var result = GetSection<T>(configuration, sectionName);
            if (result == null)
            {
                throw new InvalidOperationException($"配置节 '{sectionName}' 未找到");
            }
            return result;
        }

        /// <summary>
        /// 合并配置源
        /// </summary>
        /// <param name="builder">配置构建器</param>
        /// <param name="sources">配置源列表</param>
        /// <returns>配置构建器</returns>
        public static IConfigurationBuilder MergeConfigurationSources(
            IConfigurationBuilder builder,
            params Action<IConfigurationBuilder>[] sources)
        {
            foreach (var source in sources)
            {
                source(builder);
            }
            return builder;
        }

        /// <summary>
        /// 验证必需的配置项
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <param name="requiredKeys">必需的配置键列表</param>
        /// <returns>验证结果</returns>
        public static ConfigurationValidationResult ValidateRequiredKeys(
            IConfiguration configuration,
            params string[] requiredKeys)
        {
            var result = new ConfigurationValidationResult();

            foreach (var key in requiredKeys)
            {
                if (!Exists(configuration, key))
                {
                    result.MissingKeys.Add(key);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 配置验证结果
    /// </summary>
    public class ConfigurationValidationResult
    {
        /// <summary>
        /// 缺失的配置键
        /// </summary>
        public List<string> MissingKeys { get; } = new();

        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid => MissingKeys.Count == 0;

        /// <summary>
        /// 获取错误消息
        /// </summary>
        /// <returns>错误消息</returns>
        public string GetErrorMessage()
        {
            if (IsValid)
                return string.Empty;

            return $"以下配置项缺失: {string.Join(", ", MissingKeys)}";
        }
    }
}